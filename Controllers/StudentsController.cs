using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IActionResult> Index() =>
        View(await _db.Students.OrderByDescending(s => s.Id).ToListAsync());

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,BirthDate,Address,GuardianName,GuardianContact,GuardianEmail")] Student s, IFormFile[]? uploads)
    {
        if (!ModelState.IsValid) return View(s);
        s.StudentNumber = await NextStudentNumberAsync();
        _db.Students.Add(s);
        await _db.SaveChangesAsync();          // need s.Id for the document links
        await SaveUploadsAsync(s.Id, uploads);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await _db.Students.Include(x => x.Documents).FirstOrDefaultAsync(x => x.Id == id);
        return s is null ? NotFound() : View(s);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    public async Task<IActionResult> Edit(int id, Student s, IFormFile[]? uploads)
    {
        if (id != s.Id) return BadRequest();
        if (!ModelState.IsValid) return View(s);

        // Field-by-field copy (like FacultyController.Edit): the Edit form does not post
        // SectionId or ApplicationUserId, so a blind Update(s) would mark them Modified
        // and overwrite them with null — silently un-sectioning the student and severing
        // their linked Parent login. Only copy the columns the form actually edits.
        var existing = await _db.Students.FindAsync(id);
        if (existing is null) return NotFound();

        existing.FirstName = s.FirstName;
        existing.LastName = s.LastName;
        existing.BirthDate = s.BirthDate;
        existing.Address = s.Address;
        existing.GuardianName = s.GuardianName;
        existing.GuardianContact = s.GuardianContact;
        existing.GuardianEmail = s.GuardianEmail;

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

    // ponytail: count-based (a deleted student can reuse a number). Mirrors InquiriesController for
// consistency so walk-ins get the same admission-ID shape as online conversions.
private async Task<string> NextStudentNumberAsync()
{
    var prefix = $"{DateTime.Today.Year}-";
    var count = await _db.Students.CountAsync(s => s.StudentNumber.StartsWith(prefix));
    return $"{prefix}{count + 1:0000}";
}

public async Task<IActionResult> Details(int id)
    {
        var s = await _db.Students
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.Id == id);
        return s is null ? NotFound() : View(s);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Students.FindAsync(id);
        if (s is null) return NotFound();
        _db.Students.Remove(s);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}