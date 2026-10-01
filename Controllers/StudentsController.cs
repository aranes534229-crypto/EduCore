using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using EduCore.ViewModels;

namespace EduCore.Controllers;

/// <summary>Module 1 — Student Information Management. Admin/Registrar manage; Finance has read-only access.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar},{AppRoles.Finance}")]
public class StudentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public StudentsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index(string? q, int? gradeId, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        // Base query
        var query = _db.Students
            .Include(s => s.Person)
            .Include(s => s.Section)
            .AsQueryable();

        // Search filter — mapped columns only (FullName is computed and not translatable by EF Core)
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(s =>
                s.StudentNumber.ToLower().Contains(term) ||
                s.Person!.FirstName.ToLower().Contains(term) ||
                s.Person!.LastName.ToLower().Contains(term) ||
                s.GuardianName.ToLower().Contains(term));
        }

        // Grade level filter (via the student's section)
        if (gradeId.HasValue)
        {
            query = query.Where(s => s.Section!.GradeLevelId == gradeId.Value);
        }

        // Order
        query = query.OrderByDescending(s => s.Id);

        // Total count before paging
        var totalCount = await query.CountAsync();

        // Clamp page
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        if (page < 1) page = 1;
        if (totalPages > 0 && page > totalPages) page = totalPages;

        // Paged items
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new StudentsIndexVM
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            GradeLevelId = gradeId,
            GradeLevels = new SelectList(
                await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(), "Id", "Name")
        };
        return View(vm);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    public async Task<IActionResult> Create([Bind("StudentNumber,BirthDate,GuardianName,GuardianContact,GuardianEmail")] Student s,
        [Bind("FirstName,LastName,Email,Phone,Address")] Person person,
        IFormFile[]? uploads)
    {
        if (!ModelState.IsValid) return View(s);

        _db.Persons.Add(person);
        await _db.SaveChangesAsync();

        s.PersonId = person.Id;
        s.StudentNumber = await NextStudentNumberAsync();
        s.FirstName = person.FirstName;
        s.LastName = person.LastName;

        _db.Students.Add(s);
        await _db.SaveChangesAsync();
        await SaveUploadsAsync(s.Id, uploads);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await _db.Students
            .Include(x => x.Documents)
            .Include(x => x.Person)
            .FirstOrDefaultAsync(x => x.Id == id);
        return s is null ? NotFound() : View(s);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    public async Task<IActionResult> Edit(int id, Student s,
        [Bind("FirstName,LastName,Email,Phone,Address")] Person person,
        IFormFile[]? uploads)
    {
        if (id != s.Id) return BadRequest();
        if (!ModelState.IsValid) return View(s);

        var existing = await _db.Students
            .Include(x => x.Person)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null) return NotFound();

        existing.BirthDate = s.BirthDate;
        existing.GuardianName = s.GuardianName;
        existing.GuardianContact = s.GuardianContact;
        existing.GuardianEmail = s.GuardianEmail;

        if (existing.Person is not null)
        {
            existing.Person.FirstName = person.FirstName;
            existing.Person.LastName = person.LastName;
            existing.Person.Email = person.Email;
            existing.Person.Phone = person.Phone;
            existing.Person.Address = person.Address;
            existing.Person.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // No Person link (e.g. converted sibling inquiry) — the name fields land on the
            // Student's own columns so the record isn't stuck nameless.
            existing.FirstName = person.FirstName;
            existing.LastName = person.LastName;
        }

        await _db.SaveChangesAsync();
        await SaveUploadsAsync(existing.Id, uploads);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Saves uploaded documents (birth cert / report card / photo) and links them to the
    /// student — mirrors the inquiry application's document handling so walk-ins get the same files.</summary>
    private async Task SaveUploadsAsync(int studentId, IEnumerable<IFormFile>? uploads)
    {
        if (uploads is null) return;
        string[] allowed = { ".jpg", ".jpeg", ".png", ".pdf" };
        foreach (var file in uploads)
        {
            if (file is null || file.Length == 0) continue;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) continue;

            var relDir = $"uploads/{DateTime.Today:yyyy}/{DateTime.Today:MM}";
            var dir = Path.Combine(_env.WebRootPath, relDir);
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid():N}{ext}";   // unique on disk; original name kept for display
            using (var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
                await file.CopyToAsync(fs);

            _db.Documents.Add(new Document
            {
                StudentId = studentId,
                FileName = file.FileName,
                Path = $"{relDir}/{fileName}",
                Type = ext,
                UploadedAt = DateTime.Now
            });
        }
    }

    // ponytail: max-based — deleted/truncated rows can't cause collisions (gaps are fine).
    // Mirrors InquiriesController so walk-ins get the same admission-ID shape as online conversions.
    private async Task<string> NextStudentNumberAsync()
    {
        var prefix = $"{DateTime.Today.Year}-";
        var numbers = await _db.Students
            .Where(s => s.StudentNumber != null && s.StudentNumber.StartsWith(prefix))
            .Select(s => s.StudentNumber!)
            .ToListAsync();
        var max = numbers.Any() ? numbers.Select(n => int.TryParse(n[prefix.Length..], out var v) ? v : 0).Max() : 0;
        return $"{prefix}{max + 1:0000}";
    }

    public async Task<IActionResult> Details(int id)
    {
        var s = await _db.Students
            .Include(s => s.Documents)
            .Include(s => s.Person)
            .FirstOrDefaultAsync(s => s.Id == id);
        return s is null ? NotFound() : View(s);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Students.FindAsync(id);
        if (s is null) return NotFound();
        _db.Students.Remove(s);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}