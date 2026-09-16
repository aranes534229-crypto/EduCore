using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Module 1 — Student Information Management. Admin/Registrar only.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
public class StudentsController : Controller
{
    private readonly AppDbContext _db;
    public StudentsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync());

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,BirthDate,Address,GuardianName,GuardianContact,GuardianEmail")] Student s)
    {
        if (!ModelState.IsValid) return View(s);
        _db.Students.Add(s);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var s = await _db.Students.FindAsync(id);
        return s is null ? NotFound() : View(s);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Student s)
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
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var s = await _db.Students
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.Id == id);
        return s is null ? NotFound() : View(s);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Students.FindAsync(id);
        if (s is null) return NotFound();
        _db.Students.Remove(s);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}