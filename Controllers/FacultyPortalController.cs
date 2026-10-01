using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using EduCore.Services;
using EduCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

[Authorize(Roles = AppRoles.Faculty)]
public class FacultyPortalController : Controller
{
    private readonly FacultyScopeService _scope;
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public FacultyPortalController(FacultyScopeService scope, AppDbContext db, UserManager<ApplicationUser> users)
    {
        _scope = scope;
        _db = db;
        _users = users;
    }

    // GET /FacultyPortal
    public async Task<IActionResult> Index()
    {
        ViewData["Active"] = "FacultyPortal";
        var faculty = await _scope.GetMyFacultyAsync(User);
        if (faculty == null) return Forbid();

        var sections = await _scope.GetMySectionsAsync(faculty.Id);
        var students = await _scope.GetMyStudentsAsync(faculty.Id);

        var sectionIds = sections.Select(s => s.Id).ToList();

        // Teaching subjects count
        var teachingSubjectsCount = await _db.SectionSubjects
            .CountAsync(ss => ss.FacultyId == faculty.Id);

        // Unread messages count
        var meId = _users.GetUserId(User);
        var unreadMessages = await _db.Messages
            .CountAsync(m => m.RecipientId == meId && !m.Seen && m.StudentId != null
                && students.Select(s => s.Id).Contains(m.StudentId.Value));

        // Upcoming attendance dates (next 7 days) for my sections
        var today = DateTime.Today;
        var weekLater = today.AddDays(7);
        var upcoming = await _db.Attendance
            .Where(a => sectionIds.Contains(a.SectionId) && a.Date >= today && a.Date <= weekLater)
            .GroupBy(a => new { a.SectionId, a.Date })
            .Select(g => new AttendanceDateVM
            {
                SectionId = g.Key.SectionId,
                Date = g.Key.Date,
                SectionName = g.First().Section!.Name
            })
            .OrderBy(x => x.Date)
            .Take(10)
            .ToListAsync();

        // Recent announcements (last 5)
        var announcements = await _db.Announcements
            .OrderByDescending(a => a.PostedAt)
            .Take(5)
            .ToListAsync();

        var vm = new FacultyDashboardVM
        {
            AdvisedSectionsCount = sections.Count(s => s.AdviserId == faculty.Id),
            TeachingSubjectsCount = teachingSubjectsCount,
            TotalStudents = students.Count,
            UnreadMessages = unreadMessages,
            UpcomingAttendance = upcoming,
            RecentAnnouncements = announcements
        };

        return View(vm);
    }

    // GET /FacultyPortal/Students?search=&sectionId=&page=&pageSize=&sortColumn=&sortDesc=
    public async Task<IActionResult> Students(FacultyStudentQuery query)
    {
        ViewData["Active"] = "FacultyPortal";
        var faculty = await _scope.GetMyFacultyAsync(User);
        if (faculty == null) return Forbid();

        var sections = await _scope.GetMySectionsAsync(faculty.Id);
        var paged = await _scope.GetMyStudentsPagedAsync(faculty.Id, query);

        var studentVMs = paged.Items.Select(st => new FacultyStudentVM
        {
            Id = st.Id,
            StudentNumber = st.StudentNumber,
            FullName = st.FullName,
            SectionName = st.Section?.Name ?? "",
            GradeLevelName = st.Section?.GradeLevel?.Name ?? "",
            GuardianName = st.GuardianName,
            GuardianContact = st.GuardianContact,
            GuardianEmail = st.GuardianEmail,
            Email = st.Email,
            BirthDate = st.BirthDate
        }).ToList();

        var vm = new FacultyStudentListVM
        {
            Students = new PagedResult<FacultyStudentVM>
            {
                Items = studentVMs,
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            },
            Sections = sections,
            Query = query
        };

        return View(vm);
    }

    // GET /FacultyPortal/StudentDetail/5
    public async Task<IActionResult> StudentDetail(int id)
    {
        ViewData["Active"] = "FacultyPortal";
        var faculty = await _scope.GetMyFacultyAsync(User);
        if (faculty == null) return Forbid();

        var canAccess = await _scope.CanAccessStudentAsync(faculty.Id, id);
        if (!canAccess) return Forbid();

        var student = await _db.Students
            .Include(st => st.Person)
            .Include(st => st.Section)
                .ThenInclude(sec => sec!.GradeLevel)
            .Include(st => st.Documents)
            .FirstOrDefaultAsync(st => st.Id == id);
        if (student == null) return NotFound();

        // Grades for current section subjects
        var grades = new List<GradeSummaryVM>();
        if (student.SectionId.HasValue)
        {
            var sectionSubjects = await _db.SectionSubjects
                .Include(ss => ss.Subject)
                .Where(ss => ss.SectionId == student.SectionId.Value)
                .ToListAsync();

            var gradeRecords = await _db.Grades
                .Where(g => g.StudentId == id && sectionSubjects.Select(ss => ss.Id).Contains(g.SectionSubjectId))
                .ToListAsync();

            foreach (var ss in sectionSubjects)
            {
                var g = gradeRecords.FirstOrDefault(x => x.SectionSubjectId == ss.Id);
                grades.Add(new GradeSummaryVM
                {
                    SectionSubjectId = ss.Id,
                    SubjectName = ss.Subject?.FullName ?? ss.Subject?.Name ?? "",
                    Score = g?.Score,
                    Remarks = g?.Remarks ?? ""
                });
            }
        }

        // Attendance summary for current month
        var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        var attendances = await _db.Attendance
            .Where(a => a.StudentId == id && a.Date >= startOfMonth && a.Date <= endOfMonth)
            .ToListAsync();

        var attendanceSummary = new AttendanceSummaryVM
        {
            TotalDays = attendances.Count,
            PresentCount = attendances.Count(a => a.Status == "Present"),
            AbsentCount = attendances.Count(a => a.Status == "Absent"),
            LateCount = attendances.Count(a => a.Status == "Late")
        };

        var vm = new FacultyStudentVM
        {
            Id = student.Id,
            StudentNumber = student.StudentNumber,
            FullName = student.FullName,
            SectionName = student.Section?.Name ?? "",
            GradeLevelName = student.Section?.GradeLevel?.Name ?? "",
            GuardianName = student.GuardianName,
            GuardianContact = student.GuardianContact,
            GuardianEmail = student.GuardianEmail,
            Email = student.Email,
            BirthDate = student.BirthDate,
            Grades = grades,
            Attendance = attendanceSummary,
            Documents = student.Documents?.ToList() ?? new()
        };

        return View(vm);
    }
}