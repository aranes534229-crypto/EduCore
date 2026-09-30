using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduCore.ViewModels;

namespace EduCore.Controllers;

/// <summary>Module 4 — Faculty records. Admin manages the roster; the linked Faculty
/// account's own-portal (grades/attendance) lands with Modules 5+.</summary>
[Authorize(Roles = AppRoles.Admin)]
public class FacultyController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public FacultyController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index(string? q, bool? isActive, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        // Base query
        var query = _db.Faculty
            .Include(f => f.Person)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            // FullName is a computed property — not translatable by EF Core, so search
            // the mapped columns (EmployeeNumber, Person.FirstName/LastName/Email) instead.
            query = query.Where(f =>
                f.EmployeeNumber.ToLower().Contains(term) ||
                f.Person!.FirstName.ToLower().Contains(term) ||
                f.Person!.LastName.ToLower().Contains(term) ||
                f.Person!.Email.ToLower().Contains(term));
        }

        // Status filter (Active / Inactive)
        if (isActive.HasValue)
        {
            query = query.Where(f => f.IsActive == isActive.Value);
        }

        // Order — roster reads best alphabetical by name
        query = query
            .OrderBy(f => f.Person!.LastName)
            .ThenBy(f => f.Person!.FirstName);

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

        var vm = new FacultyIndexVM
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            IsActive = isActive
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("EmployeeNumber,HireDate,IsActive")] Faculty faculty,
        [Bind("FirstName,LastName,Email,Phone,Address")] Person person)
    {
        if (!ModelState.IsValid) return View(faculty);

        if (!string.IsNullOrWhiteSpace(person.Email))
        {
            var existingUser = await _users.FindByEmailAsync(person.Email);
            if (existingUser is not null)
                ModelState.AddModelError(nameof(person.Email), "An account already uses this email.");
        }

        if (!ModelState.IsValid) return View(faculty);

        _db.Persons.Add(person);
        await _db.SaveChangesAsync();

        faculty.PersonId = person.Id;

        // Create a Faculty login for them so the portal is reachable once Modules 5+ ship.
        if (!string.IsNullOrWhiteSpace(person.Email))
        {
            var acct = new ApplicationUser
            {
                UserName = person.Email,
                Email = person.Email,
                PersonId = person.Id,
                EmailConfirmed = true
            };
            var result = await _users.CreateAsync(acct, "Teacher123!");
            if (result.Succeeded)
            {
                await _users.AddToRoleAsync(acct, AppRoles.Faculty);
            }
            else
            {
                ModelState.AddModelError(nameof(person.Email),
                    string.Join(" ", result.Errors.Select(e => e.Description)));
                return View(faculty);
            }
        }

        _db.Faculty.Add(faculty);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var faculty = await _db.Faculty
            .Include(f => f.Person)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (faculty is null) return NotFound();
        return View(faculty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Faculty faculty,
        [Bind("FirstName,LastName,Email,Phone,Address")] Person person)
    {
        if (id != faculty.Id) return BadRequest();
        if (!ModelState.IsValid) return View(faculty);

        var existing = await _db.Faculty
            .Include(f => f.Person)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (existing is null) return NotFound();

        existing.EmployeeNumber = faculty.EmployeeNumber;
        existing.HireDate = faculty.HireDate;
        existing.IsActive = faculty.IsActive;

        if (existing.Person is not null)
        {
            existing.Person.FirstName = person.FirstName;
            existing.Person.LastName = person.LastName;
            existing.Person.Email = person.Email;
            existing.Person.Phone = person.Phone;
            existing.Person.Address = person.Address;
            existing.Person.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var faculty = await _db.Faculty
            .Include(f => f.Person)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (faculty is null) return NotFound();
        return View(faculty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var faculty = await _db.Faculty
            .Include(f => f.Person)
                .ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (faculty is not null)
        {
            // Clear references from Sections (adviser) and SectionSubjects (teacher)
            var sections = await _db.Sections.Where(s => s.AdviserId == faculty.Id).ToListAsync();
            foreach (var s in sections) s.AdviserId = null;

            var sectionSubjects = await _db.SectionSubjects.Where(ss => ss.FacultyId == faculty.Id).ToListAsync();
            foreach (var ss in sectionSubjects) ss.FacultyId = null;

            await _db.SaveChangesAsync();

            // Remove linked ApplicationUser (login account) if exists
            if (faculty.Person?.User is not null)
            {
                var user = faculty.Person.User;
                var result = await _users.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Failed to delete linked account: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                    return RedirectToAction(nameof(Index));
                }
            }

            _db.Faculty.Remove(faculty);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}