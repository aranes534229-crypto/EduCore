using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Module 2 — Enrollment. A student is enrolled into a grade for a school year;
/// status is Unscheduled until the Registrar places them in a section (see SectionsController),
/// which flips it to Scheduled.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
public class EnrollmentsController : Controller
{
    private readonly AppDbContext _db;
    public EnrollmentsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.GradeLevel)
            .Include(e => e.SchoolYear)
            .OrderByDescending(e => e.Id)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateChoicesAsync(true);
        return View(new Enrollment());
    }

    [HttpPost, ValidateAntiForgeryToken]
    // Bind-exclude Status so it can only change via the scheduling flow (SectionController),
    // never from a form. A forged POST could otherwise append &Status=Scheduled.
    public async Task<IActionResult> Create([Bind("Id,StudentId,GradeLevelId,SchoolYearId,Notes")] Enrollment e)
    {
        if (!ModelState.IsValid)
        {
            await PopulateChoicesAsync(true, e.GradeLevelId, e.SchoolYearId);
            return View(e);
        }

        if (await DuplicateAsync(e, 0))
            ModelState.AddModelError(nameof(Enrollment.StudentId), "This student already has an application for that school year.");

        if (!ModelState.IsValid)
        {
            await PopulateChoicesAsync(true, e.GradeLevelId, e.SchoolYearId);
            return View(e);
        }

        _db.Enrollments.Add(e);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var e = await _db.Enrollments
            .Include(x => x.Student)
            .Include(x => x.GradeLevel)
            .Include(x => x.SchoolYear)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();
        return View(e);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var e = await _db.Enrollments.FindAsync(id);
        if (e is null) return NotFound();
        await PopulateChoicesAsync(false, e.GradeLevelId, e.SchoolYearId);
        return View(e);
    }

    [HttpPost, ValidateAntiForgeryToken]
    // Status is not bound — it's flow-driven (Unscheduled until the Registrar places the
    // student in a section, then Scheduled via SectionController.AssignStudent/RemoveStudent).
    public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,GradeLevelId,SchoolYearId,Notes")] Enrollment form)
    {
        var e = await _db.Enrollments.FindAsync(id);
        if (e is null) return NotFound();

        if (!ModelState.IsValid || await DuplicateAsync(form, id))
        {
            await PopulateChoicesAsync(false, form.GradeLevelId, form.SchoolYearId);
            return View(form);
        }

        e.StudentId = form.StudentId;
        e.GradeLevelId = form.GradeLevelId;
        e.SchoolYearId = form.SchoolYearId;
        e.Notes = form.Notes;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ponytail: guarded against a student+year collision; per-year sequences would matter if the
    // same student could hold multiple applications in a year — currently one is enforced.
    private async Task<bool> DuplicateAsync(Enrollment form, int excludeId)
        => await _db.Enrollments.AnyAsync(x => x.StudentId == form.StudentId
            && x.SchoolYearId == form.SchoolYearId && x.Id != excludeId);

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Enrollments.FindAsync(id);
        if (e is not null)
        {
            _db.Enrollments.Remove(e);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateChoicesAsync(bool filterUnenrolled = false, int? gradeLevelId = null, int? schoolYearId = null)
    {
        IQueryable<Student> students = _db.Students;
        if (filterUnenrolled)
            students = students.Where(s => !_db.Enrollments.Any(e => e.StudentId == s.Id));
        ViewBag.Students = new SelectList(
            await students.OrderByDescending(s => s.Id).ToListAsync(),
            "Id", "StudentNumber");
        ViewBag.GradeLevels = new SelectList(
            await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(), "Id", "Name", gradeLevelId);
        ViewBag.SchoolYears = new SelectList(
            await _db.SchoolYears.OrderByDescending(y => y.StartDate).ToListAsync(), "Id", "Name", schoolYearId);
    }
}