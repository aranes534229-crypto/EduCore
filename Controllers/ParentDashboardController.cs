using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using EduCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

[Authorize(Roles = AppRoles.Parent)]
public class ParentDashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ParentDashboardController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var meId = _users.GetUserId(User);
        var me = await _users.GetUserAsync(User);
        var meEmail = me?.Email?.ToLower();

        // Get parent's children: linked via Person.UserId OR matching GuardianEmail
        var children = await _db.Students
            .Include(s => s.Person)
            .Include(s => s.Section).ThenInclude(sec => sec!.GradeLevel)
            .Where(s => s.Person != null && s.Person.User != null && s.Person.User.Id == meId
                     || (meEmail != null && s.GuardianEmail != null && s.GuardianEmail.ToLower() == meEmail))
            .ToListAsync();

        var studentIds = children.Select(s => s.Id).ToList();

        // Active school year
        var activeYear = await _db.SchoolYears.FirstOrDefaultAsync(y => y.IsActive);
        var activeYearId = activeYear?.Id ?? 0;

        // Build child summaries
        var childSummaries = new List<ChildSummaryVM>();
        decimal totalBalance = 0;

        foreach (var child in children)
        {
            // Outstanding balance for active year
            var invoicesQuery = _db.Invoices.Where(i => i.StudentId == child.Id);
            if (activeYear != null) invoicesQuery = invoicesQuery.Where(i => i.SchoolYearId == activeYear.Id);
            var invoices = await invoicesQuery
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .ToListAsync();
            var balance = invoices.Sum(i => i.Lines.Sum(l => l.Amount) - i.Payments.Sum(p => p.Amount));

            // Unread messages for this child
            var unreadMsg = await _db.Messages
                .CountAsync(m => m.RecipientId == meId && m.StudentId == child.Id && !m.Seen);

            // Current enrollment status
            var enrollment = await _db.Enrollments
                .Where(e => e.StudentId == child.Id && e.SchoolYearId == activeYearId)
                .FirstOrDefaultAsync();

            childSummaries.Add(new ChildSummaryVM
            {
                StudentId = child.Id,
                StudentNumber = child.StudentNumber,
                FullName = child.FullName,
                GradeLevel = child.Section?.GradeLevel?.Name ?? "—",
                SectionName = child.Section?.Name ?? "—",
                OutstandingBalance = balance,
                UnreadMessages = unreadMsg,
                EnrollmentStatus = enrollment?.Status
            });
            totalBalance += balance;
        }

        // Aggregate stats
        var unreadMessages = await _db.Messages
            .CountAsync(m => m.RecipientId == meId && studentIds.Contains(m.StudentId) && !m.Seen);

        var pendingInquiries = await _db.Inquiries
            .CountAsync(i => i.CreatedByUserId == meId && i.Status == InquiryStatus.New);

        var activeEnrollments = await _db.Enrollments
            .CountAsync(e => studentIds.Contains(e.StudentId) && e.Status == EnrollmentStatus.Scheduled);

        // Recent activity
        var recentMessages = await _db.Messages
            .Where(m => (m.SenderId == meId || m.RecipientId == meId) && studentIds.Contains(m.StudentId))
            .OrderByDescending(m => m.SentAt)
            .Take(5)
            .ToListAsync();

        var recentInquiries = await _db.Inquiries
            .Where(i => i.CreatedByUserId == meId)
            .OrderByDescending(i => i.DateCreated)
            .Take(5)
            .ToListAsync();

        var recentAnnouncements = await _db.Announcements
            .OrderByDescending(a => a.PostedAt)
            .Take(5)
            .ToListAsync();

        var vm = new ParentDashboardVM
        {
            Children = childSummaries,
            TotalOutstandingBalance = totalBalance,
            UnreadMessages = unreadMessages,
            PendingInquiries = pendingInquiries,
            ActiveEnrollments = activeEnrollments,
            RecentMessages = recentMessages,
            RecentInquiries = recentInquiries,
            RecentAnnouncements = recentAnnouncements
        };
        return View(vm);
    }
}