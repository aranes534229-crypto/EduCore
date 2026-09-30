using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using EduCore.ViewModels;

namespace EduCore.Controllers;

/// <summary>Module 2 — Enrollment. A student is enrolled into a grade for a school year;
/// status is Unscheduled until the Registrar places them in a section (see SectionsController),
/// which flips it to Scheduled.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
public class EnrollmentsController : Controller
{
    private readonly AppDbContext _db;
    public EnrollmentsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, EnrollmentStatus? status, int? gradeId, int? schoolYearId, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        // Base query
        var query = _db.Enrollments
            .Include(e => e.Student).ThenInclude(s => s.Person)
            .Include(e => e.GradeLevel)
            .Include(e => e.SchoolYear)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            // FullName is a computed property — not translatable by EF Core, so search
            // the mapped columns (StudentNumber, Person.FirstName/LastName) instead.
            query = query.Where(e =>
                e.Student.StudentNumber.ToLower().Contains(term) ||
                e.Student.Person!.FirstName.ToLower().Contains(term) ||
                e.Student.Person!.LastName.ToLower().Contains(term));
        }

        // Status filter
        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        // Grade level filter
        if (gradeId.HasValue)
        {
            query = query.Where(e => e.GradeLevelId == gradeId.Value);
        }

        // School year filter
        if (schoolYearId.HasValue)
        {
            query = query.Where(e => e.SchoolYearId == schoolYearId.Value);
        }

        // Order
        query = query.OrderByDescending(e => e.Id);

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

        var vm = new EnrollmentsIndexVM
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            Status = status,
            GradeLevelId = gradeId,
            SchoolYearId = schoolYearId,
            GradeLevels = new SelectList(
                await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(), "Id", "Name"),
            SchoolYears = new SelectList(
                await _db.SchoolYears.OrderByDescending(y => y.StartDate).ToListAsync(), "Id", "Name")
        };
        return View(vm);
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
            .Include(x => x.Student).ThenInclude(s => s.Person)
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
        IQueryable<Student> students = _db.Students.Include(s => s.Person);
        if (filterUnenrolled)
            students = students.Where(s => !_db.Enrollments.Any(e => e.StudentId == s.Id));
        ViewBag.Students = new SelectList(
            (await students.OrderByDescending(s => s.Id).ToListAsync())
                .Select(s => new { s.Id, Display = s.Person == null ? s.StudentNumber : $"{s.StudentNumber} — {s.Person.FullName}" })
                .ToList(),
            "Id", "Display");
        ViewBag.GradeLevels = new SelectList(
            await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(), "Id", "Name", gradeLevelId);
        ViewBag.SchoolYears = new SelectList(
            await _db.SchoolYears.OrderByDescending(y => y.StartDate).ToListAsync(), "Id", "Name", schoolYearId);
    }
}