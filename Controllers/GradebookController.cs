using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduCore.ViewModels;

namespace EduCore.Controllers;

/// <summary>Module 5 — grade & attendance entry. Faculty enters records only for the
/// sections they teach; Admin may enter anywhere.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Faculty}")]
public class GradebookController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public GradebookController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    private async Task<int?> MyIdAsync() =>
        (await _db.Faculty.FirstOrDefaultAsync(f => f.ApplicationUserId == _users.GetUserId(User)))?.Id;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        IQueryable<Section> q = _db.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.Adviser)
            .Include(s => s.Subjects).ThenInclude(ss => ss.Subject)
            .Include(s => s.Subjects).ThenInclude(ss => ss.Faculty);

        if (User.IsInRole(AppRoles.Faculty))
        {
            var me = await MyIdAsync();
            q = q.Where(s => me != null && (s.AdviserId == me ||
                s.Subjects.Any(ss => ss.FacultyId == me)));
        }

        var sections = await q.OrderBy(s => s.GradeLevel.SortOrder).ThenBy(s => s.Name).ToListAsync();
        return View(sections);
    }

    [HttpGet]
    public async Task<IActionResult> Grades(int sectionSubjectId)
    {
        var sectionSubject = await AuthorizeSectionSubjectAsync(sectionSubjectId);
        if (sectionSubject is null) return Forbid();

        ViewBag.Ss = sectionSubject;

        var rows = await _db.Students
            .Where(st => st.SectionId == sectionSubject.SectionId)
            .OrderBy(st => st.LastName)
            .ToListAsync();
        var existing = await _db.Grades
            .Where(g => g.SectionSubjectId == sectionSubjectId).ToListAsync();

        var model = rows.Select(st =>
        {
            var g = existing.FirstOrDefault(x => x.StudentId == st.Id);
            return new GradeRowVM
            {
                StudentId = st.Id,
                Score = g?.Score,
                Remarks = g?.Remarks ?? ""
            };
        }).ToList();

        ViewBag.StudentNames = rows.ToDictionary(s => s.Id, s => s.FullName);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Grades(int sectionSubjectId, List<GradeRowVM> rows)
    {
        var sectionSubject = await AuthorizeSectionSubjectAsync(sectionSubjectId);
        if (sectionSubject is null) return Forbid();

        foreach (var row in rows)   // one row per student; upsert their grade for this subject
        {
            // Skip rows left blank: null means "not graded yet", not a zero score.
            if (!row.Score.HasValue) continue;

            var grade = await _db.Grades.FirstOrDefaultAsync(
                g => g.SectionSubjectId == sectionSubjectId && g.StudentId == row.StudentId);
            if (grade is null)
                _db.Grades.Add(new Grade
                {
                    SectionSubjectId = sectionSubjectId,
                    StudentId = row.StudentId,
                    Score = row.Score.Value,
                    Remarks = row.Remarks?.Trim() ?? ""
                });
            else
            {
                grade.Score = row.Score.Value;
                grade.Remarks = row.Remarks?.Trim() ?? "";
            }
        }
        await _db.SaveChangesAsync();
        TempData["Saved"] = "Grades saved.";
        return RedirectToAction(nameof(Grades), new { sectionSubjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Attendance(int sectionId, DateTime? date)
    {
        if (!await AuthorizeSectionAsync(sectionId)) return Forbid();

        var day = (date ?? DateTime.Today).Date;
        var records = await _db.Attendance
            .Where(a => a.SectionId == sectionId && a.Date == day).ToListAsync();
        var students = await _db.Students
            .Where(st => st.SectionId == sectionId).OrderBy(st => st.LastName).ToListAsync();

        var model = students.Select(st =>
        {
            var a = records.FirstOrDefault(x => x.StudentId == st.Id);
            return new AttendanceRowVM { StudentId = st.Id, Status = a?.Status ?? "Present" };
        }).ToList();

        ViewBag.SectionId = sectionId;
        ViewBag.Date = day;
        ViewBag.StudentNames = students.ToDictionary(s => s.Id, s => s.FullName);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Attendance(int sectionId, DateTime date, List<AttendanceRowVM> rows)
    {
        if (!await AuthorizeSectionAsync(sectionId)) return Forbid();

        var day = date.Date;
        foreach (var row in rows)
        {
            var a = await _db.Attendance.FirstOrDefaultAsync(
                x => x.SectionId == sectionId && x.StudentId == row.StudentId && x.Date == day);
            if (a is null)
                _db.Attendance.Add(new Attendance
                { SectionId = sectionId, StudentId = row.StudentId, Date = day, Status = row.Status });
            else
                a.Status = row.Status;
        }
        await _db.SaveChangesAsync();
        TempData["Saved"] = "Attendance saved.";
        return RedirectToAction(nameof(Attendance), new { sectionId, date });
    }

    private async Task<SectionSubject?> AuthorizeSectionSubjectAsync(int id)
    {
        var ss = await _db.SectionSubjects
            .Include(ss => ss.Section)
            .Include(ss => ss.Subject)
            .Include(ss => ss.Faculty)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (ss is null) return null;
        if (User.IsInRole(AppRoles.Admin)) return ss;
        return ss.FacultyId == await MyIdAsync() ? ss : null;
    }

    private async Task<bool> AuthorizeSectionAsync(int sectionId)
    {
        if (User.IsInRole(AppRoles.Admin)) return true;
        var me = await MyIdAsync();
        var section = await _db.Sections
            .Include(s => s.Subjects)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
        return section is not null && (section.AdviserId == me || section.Subjects.Any(ss => ss.FacultyId == me));
    }
}