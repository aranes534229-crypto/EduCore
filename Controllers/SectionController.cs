using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Module 5 — sections + who teaches/attends them. Admin and Registrar manage
/// the roster and teacher assignments; grade/attendance entry lives in GradebookController.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
public class SectionsController : Controller
{
    private readonly AppDbContext _db;

    public SectionsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.Adviser)
            .Include(s => s.Students)
            .OrderBy(s => s.GradeLevel.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateChoicesAsync();
        return View(new Section());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Section section)
    {
        if (!ModelState.IsValid)
        {
            await PopulateChoicesAsync(section.GradeLevelId);
            return View(section);
        }

        // One active section per grade level for the year keeps a student's grade = their section's grade.
        if (await _db.Sections.AnyAsync(s => s.GradeLevelId == section.GradeLevelId && s.SchoolYearId == section.SchoolYearId))
            ModelState.AddModelError(nameof(Section.Name), "A section already exists for this grade level and school year.");

        if (!ModelState.IsValid)
        {
            await PopulateChoicesAsync(section.GradeLevelId);
            return View(section);
        }

        _db.Sections.Add(section);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var section = await _db.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.SchoolYear)
            .Include(s => s.Adviser)
            .Include(s => s.Students)
            .Include(s => s.Subjects).ThenInclude(ss => ss.Subject)
            .Include(s => s.Subjects).ThenInclude(ss => ss.Faculty)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (section is null) return NotFound();

        // Subject picker shows only subjects not already meeting in this section.
        var assigned = section.Subjects.Select(ss => ss.SubjectId).ToHashSet();
        ViewBag.Subjects = new SelectList(
            await _db.Subjects.Where(x => !assigned.Contains(x.Id)).OrderBy(x => x.Name).ToListAsync(),
            "Id", "Name");
        ViewBag.Faculty = new SelectList(
            await _db.Faculty.Where(f => f.IsActive).OrderBy(f => f.LastName).ToListAsync(),
            "Id", "FullName");

        // Assignable students: anyone not yet placed in a section.
        // ponytail: student grade = their section's grade level (no GradeLevelId on Student);
        // grade-at-application is captured in Module 2 Enrollment, which places via section.
        ViewBag.Pool = await _db.Students
            .Where(st => st.SectionId == null)
            .OrderBy(st => st.LastName)
            .ToListAsync();

        return View(section);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignStudent(int id, int studentId)
    {
        var student = await _db.Students.FindAsync(studentId);
        var section = await _db.Sections.FindAsync(id);
        if (student is not null && section is not null && student.SectionId is null)
        {
            student.SectionId = id;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStudent(int id, int studentId)
    {
        var student = await _db.Students.FindAsync(studentId);
        if (student is not null && student.SectionId == id)
        {
            student.SectionId = null;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSubject(int id, int subjectId, int facultyId)
    {
        if (!await _db.SectionSubjects.AnyAsync(ss => ss.SectionId == id && ss.SubjectId == subjectId))
        {
            _db.SectionSubjects.Add(new SectionSubject
            {
                SectionId = id,
                SubjectId = subjectId,
                FacultyId = facultyId
            });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSubject(int id, int sectionSubjectId)
    {
        var ss = await _db.SectionSubjects.FindAsync(sectionSubjectId);
        if (ss is not null && ss.SectionId == id)
        {
            _db.SectionSubjects.Remove(ss);
            await _db.SaveChangesAsync();   // grades on this subject are removed with it (cascade)
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var section = await _db.Sections.FindAsync(id);
        if (section is not null)
        {
            _db.Sections.Remove(section);   // students keep their row; SectionId set null
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateChoicesAsync(int? gradeLevelId = null, int? schoolYearId = null)
    {
        ViewBag.GradeLevels = new SelectList(
            await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(), "Id", "Name", gradeLevelId);
        ViewBag.SchoolYears = new SelectList(
            await _db.SchoolYears.OrderByDescending(y => y.StartDate).ToListAsync(), "Id", "Name", schoolYearId);
        ViewBag.Advisers = new SelectList(
            await _db.Faculty.Where(f => f.IsActive).OrderBy(f => f.LastName).ToListAsync(), "Id", "FullName");
    }
}