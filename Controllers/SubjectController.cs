using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class SubjectsController : Controller
{
    private readonly AppDbContext _db;

    public SubjectsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Subjects.OrderBy(s => s.Name).ToListAsync());

    [HttpGet]
    public IActionResult Create() => View(new Subject());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Subject subject)
    {
        if (!ModelState.IsValid) return View(subject);
        if (await _db.Subjects.AnyAsync(s => s.Code == subject.Code))
            ModelState.AddModelError(nameof(Subject.Code), "This code is already in use.");
        if (!ModelState.IsValid) return View(subject);

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var subject = await _db.Subjects.FindAsync(id);
        if (subject is not null)
        {
            // Only deletable if it has no subject-in-section assignments.
            if (await _db.SectionSubjects.AnyAsync(ss => ss.SubjectId == id))
                TempData["Error"] = "This subject is assigned to a section and cannot be removed.";
            else
            {
                _db.Subjects.Remove(subject);
                await _db.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(Index));
    }
}