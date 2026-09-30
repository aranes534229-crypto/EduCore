using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduCore.ViewModels;

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
        (await _db.Faculty
            .Include(f => f.Person)
            .FirstOrDefaultAsync(f => f.Person!.User!.Id == _users.GetUserId(User)))?.Id;

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
    public async Task<IActionResult> Enrollment(string? q, int? schoolYearId, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        var years = await _db.SchoolYears.OrderByDescending(y => y.StartDate).ToListAsync();
        var year = schoolYearId.HasValue
            ? await _db.SchoolYears.FirstOrDefaultAsync(y => y.Id == schoolYearId)
            : years.FirstOrDefault(y => y.IsActive) ?? years.FirstOrDefault();

        var vm = new EnrollmentReportVM
        {
            SchoolYears = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(years, "Id", "Name", year?.Id),
            SelectedYear = year?.Id,
            YearName = year?.Name ?? "(none)",
            Search = q,
            Totals = new Dictionary<EnrollmentStatus, int>()
        };

        if (year is null)
        {
            vm.Total = 0;
            return View(vm);
        }

        // Group the filtered set by status; status counts come from the grouped rows below.
        var query = _db.Enrollments
            .Where(e => e.SchoolYearId == year.Id)
            .Include(e => e.Student).ThenInclude(s => s.Person)
            .Include(e => e.GradeLevel)
            .AsQueryable();

        // Search filter — FullName is a computed property — not translatable by EF Core,
        // so search the mapped columns (StudentNumber, Person.FirstName/LastName) instead.
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(e =>
                e.Student.StudentNumber.ToLower().Contains(term) ||
                e.Student.Person!.FirstName.ToLower().Contains(term) ||
                e.Student.Person!.LastName.ToLower().Contains(term));
        }

        // Order
        query = query
            .OrderBy(e => e.GradeLevel.SortOrder)
            .ThenBy(e => e.Student.Person!.LastName)
            .ThenBy(e => e.Student.Person!.FirstName);

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

        // Tiles reflect the selected year for ALL its enrollments, regardless of search/paging.
        var yearItems = await _db.Enrollments
            .Where(e => e.SchoolYearId == year.Id)
            .Select(e => e.Status)
            .ToListAsync();

        vm.Items = items;
        vm.Page = page;
        vm.PageSize = pageSize;
        vm.TotalCount = totalCount;
        vm.Totals = Enum.GetValues<EnrollmentStatus>()
            .ToDictionary(s => s, s => yearItems.Count(i => i == s));
        vm.Total = yearItems.Count;

        return View(vm);
    }

    // GET /Reports/Money  (Admin | Finance)
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Finance}")]
    [HttpGet]
    public async Task<IActionResult> Money(string? q, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        // Global figures stay global — computed from ALL invoices, not the filtered set.
        var invoices = await _db.Invoices
            .Include(i => i.Student).ThenInclude(s => s.Person)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .ToListAsync();

        var totalBilled = invoices.Sum(i => i.Total);
        // SQLite stores decimal as text and can't SUM() it in SQL; cast through double (REAL) for the aggregate.
        var totalCollected = (decimal?)(double?)await _db.Payments.SumAsync(p => (double?)p.Amount) ?? 0m;
        // per-invoice Outstanding = Total - Paid; sum the computed balance, never store it.
        var totalOutstanding = invoices.Sum(i => i.Balance);
        var totalExpenses = (decimal?)(double?)await _db.Expenses.SumAsync(e => (double?)e.Amount) ?? 0m;

        // Search filter — FullName is a computed property — not translatable by EF Core,
        // so search the mapped columns (Invoice.Number, StudentNumber, Person names) instead.
        var filtered = invoices.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            filtered = filtered.Where(i =>
                i.Number != null && i.Number.ToLower().Contains(term) ||
                i.Student.StudentNumber.ToLower().Contains(term) ||
                i.Student.Person!.FirstName.ToLower().Contains(term) ||
                i.Student.Person!.LastName.ToLower().Contains(term));
        }

        // Total count before paging
        var list = filtered.ToList();
        var totalCount = list.Count;

        // Clamp page
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        if (page < 1) page = 1;
        if (totalPages > 0 && page > totalPages) page = totalPages;

        // Paged items
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var vm = new MoneyReportVM
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            TotalBilled = totalBilled,
            TotalCollected = totalCollected,
            TotalOutstanding = totalOutstanding,
            TotalExpenses = totalExpenses
        };
        return View(vm);
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
                .Include(s => s.Person)
                .OrderBy(s => s.Person!.LastName)
                .ThenBy(s => s.Person!.FirstName)
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
