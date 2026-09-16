using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IActionResult> Index()
    {
        var list = await _db.Faculty.OrderBy(f => f.LastName).ThenBy(f => f.FirstName).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,Contact,IsActive")] Faculty faculty)
    {
        if (!ModelState.IsValid) return View(faculty);

        if (!string.IsNullOrWhiteSpace(faculty.Email) && (await _users.FindByEmailAsync(faculty.Email)) is not null)
            ModelState.AddModelError(nameof(Faculty.Email), "An account already uses this email.");

        if (!ModelState.IsValid) return View(faculty);

        // Create a Faculty login for them so the portal is reachable once Modules 5+ ship.
        if (!string.IsNullOrWhiteSpace(faculty.Email))
        {
            var acct = new ApplicationUser
            {
                UserName = faculty.Email,
                Email = faculty.Email,
                DisplayName = faculty.FullName,
                EmailConfirmed = true
            };
            var result = await _users.CreateAsync(acct, "Teacher123!");
            if (result.Succeeded)
            {
                await _users.AddToRoleAsync(acct, AppRoles.Faculty);
                faculty.ApplicationUserId = acct.Id;
            }
            else
            {
                ModelState.AddModelError(nameof(Faculty.Email),
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
        var faculty = await _db.Faculty.FindAsync(id);
        if (faculty is null) return NotFound();
        return View(faculty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Faculty faculty)
    {
        if (id != faculty.Id) return BadRequest();
        if (!ModelState.IsValid) return View(faculty);

        var existing = await _db.Faculty.FindAsync(id);
        if (existing is null) return NotFound();

        existing.FirstName = faculty.FirstName;
        existing.LastName = faculty.LastName;
        existing.Email = faculty.Email;
        existing.Contact = faculty.Contact;
        existing.IsActive = faculty.IsActive;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var faculty = await _db.Faculty.FindAsync(id);
        if (faculty is null) return NotFound();
        return View(faculty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var faculty = await _db.Faculty.FindAsync(id);
        if (faculty is not null)
        {
            _db.Faculty.Remove(faculty);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}