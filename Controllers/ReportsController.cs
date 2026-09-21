using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>
/// Module 9 — Reports &amp; Analytics. No new tables; each role gets the slice of
/// the existing data it is allowed to see (per CLAUDE.md role scope).
///   Admin     → all reports
///   Registrar → enrollment reports only
///   Finance   → money reports only
///   Faculty   → class-results report only
/// </summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar},{AppRoles.Finance},{AppRoles.Faculty}")]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ReportsController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    private async Task<int?> MyFacultyIdAsync() =>
        (await _db.Faculty.FirstOrDefaultAsync(f => f.ApplicationUserId == _users.GetUserId(User)))?.Id;

    // GET /Reports
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // One card per role-scoped report the current user is allowed to see.
        var cards = new List<ReportCard>();

        if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Registrar))
        {
            cards.Add(new ReportCard
            {
                Title = "Enrollment",
                Description = "Applications, approvals, and rejections by status.",
                Action = nameof(Enrollment)
            });
        }

        if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Finance))
        {
            cards.Add(new ReportCard
            {
                Title = "Money",
                Description = "Total billed, collected, outstanding, and expenses.",
                Action = nameof(Money)
            });
        }

        if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Faculty))
        {
            cards.Add(new ReportCard
            {
                Title = "Class Results",
                Description = "Per-subject averages for the classes you teach.",
                Action = nameof(ClassResults)
            });
        }

        ViewBag.Cards = cards;
        return View();
    }

    // GET /Reports/Enrollment  (Admin | Registrar)
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    [HttpGet]
    public async Task<IActionResult> Enrollment(int? schoolYearId)
    {
        var years = await _db.SchoolYears.OrderByDescending(y => y.StartDate).ToListAsync();
        var year = schoolYearId.HasValue
            ? await _db.SchoolYears.FirstOrDefaultAsync(y => y.Id == schoolYearId)
            : years.FirstOrDefault(y => y.IsActive) ?? years.FirstOrDefault();

        ViewBag.SchoolYears = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(years, "Id", "Name", year?.Id);
        ViewBag.SelectedYear = year?.Id;
        ViewBag.YearName = year?.Name ?? "(none)";

        if (year is null)
        {
            ViewBag.Totals = new Dictionary<EnrollmentStatus, int>();
            ViewBag.Total = 0;
            return View(new List<Enrollment>());
        }

        // Group the filtered set by status; status counts come from the grouped rows below.
        var items = await _db.Enrollments
            .Where(e => e.SchoolYearId == year.Id)
            .Include(e => e.Student)
            .Include(e => e.GradeLevel)
            .OrderBy(e => e.GradeLevel.SortOrder).ThenBy(e => e.Student.LastName)
            .ToListAsync();

        ViewBag.Totals = Enum.GetValues<EnrollmentStatus>()
            .ToDictionary(s => s, s => items.Count(i => i.Status == s));
        ViewBag.Total = items.Count;

        return View(items);
    }

    // GET /Reports/Money  (Admin | Finance)
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Finance}")]
    [HttpGet]
    public async Task<IActionResult> Money()
    {
        // ponytail: one SumAsync per figure; these tables are small so extra round-trips < a grouped query here.
        var invoices = await _db.Invoices
            .Include(i => i.Student)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .ToListAsync();

        var totalBilled = invoices.Sum(i => i.Total);
        // SQLite stores decimal as text and can't SUM() it in SQL; cast through double (REAL) for the aggregate.
        var totalCollected = (decimal?)(double?)await _db.Payments.SumAsync(p => (double?)p.Amount) ?? 0m;
        // per-invoice Outstanding = Total - Paid; sum the computed balance, never store it.
        var totalOutstanding = invoices.Sum(i => i.Balance);
        var totalExpenses = (decimal?)(double?)await _db.Expenses.SumAsync(e => (double?)e.Amount) ?? 0m;

        ViewBag.TotalBilled = totalBilled;
        ViewBag.TotalCollected = totalCollected;
        ViewBag.TotalOutstanding = totalOutstanding;
        ViewBag.TotalExpenses = totalExpenses;
        ViewBag.Net = totalOutstanding - totalExpenses;

        return View(invoices);
    }

    // GET /Reports/ClassResults  (Admin | Faculty)
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Faculty}")]
    [HttpGet]
    public async Task<IActionResult> ClassResults(int? sectionSubjectId)
    {
        // A Faculty user may only open a subject they teach (reuses the Gradebook scope rule).
        IQueryable<SectionSubject> q = _db.SectionSubjects
            .Include(ss => ss.Section).ThenInclude(s => s.GradeLevel)
            .Include(ss => ss.Subject)
            .Include(ss => ss.Faculty)
            .Include(ss => ss.Section).ThenInclude(s => s.SchoolYear);

        if (User.IsInRole(AppRoles.Faculty))
        {
            var me = await MyFacultyIdAsync();
            if (me is null) return Forbid();
            q = q.Where(ss => ss.FacultyId == me);
        }

        var options = await q.OrderBy(ss => ss.Section.SchoolYear.IsActive ? 0 : 1)
            .ThenBy(ss => ss.Section.GradeLevel.SortOrder)
            .ThenBy(ss => ss.Section.Name)
            .ThenBy(ss => ss.Subject.Name)
            .ToListAsync();

        if (options.Count > 0)
        {
            var chosen = sectionSubjectId.HasValue
                ? options.FirstOrDefault(ss => ss.Id == sectionSubjectId.Value) ?? options[0]
                : options[0];

            var students = await _db.Students
                .Where(s => s.SectionId == chosen.SectionId)
                .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
                .ToListAsync();
            var grades = await _db.Grades
                .Where(g => g.SectionSubjectId == chosen.Id)
                .ToListAsync();

            var rows = students.Select(s => new StudentResult
            {
                StudentName = s.FullName,
                Score = grades.FirstOrDefault(g => g.StudentId == s.Id)?.Score,
                Remarks = grades.FirstOrDefault(g => g.StudentId == s.Id)?.Remarks ?? ""
            }).ToList();

            var scored = rows.Where(r => r.Score.HasValue).Select(r => r.Score!.Value).ToList();
            ViewBag.Average = scored.Count > 0 ? (decimal?)scored.Average() : null;
            ViewBag.HavingScores = scored.Count;
            ViewBag.Query = chosen;
            ViewBag.Students = students;
            ViewBag.Rows = rows;
        }

        ViewBag.Options = options;
        ViewBag.SelectedId = (options.Count > 0)
            ? (sectionSubjectId.HasValue && options.Any(ss => ss.Id == sectionSubjectId.Value) ? sectionSubjectId.Value : options[0].Id)
            : 0;

        return View();
    }

    public class ReportCard
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Action { get; set; } = "";
    }

    public class StudentResult
    {
        public string StudentName { get; set; } = "";
        public int? Score { get; set; }
        public string Remarks { get; set; } = "";
    }
}
