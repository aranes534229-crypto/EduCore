using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using EduCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Finance}")]
public class FinanceDashboardController : Controller
{
    private readonly AppDbContext _db;

    public FinanceDashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        // Active school year
        var activeYear = await _db.SchoolYears.FirstOrDefaultAsync(y => y.IsActive);
        var activeYearId = activeYear?.Id ?? 0;

        // Load invoices for active year with lines and payments (in memory to avoid aggregate-on-aggregate)
        var invoices = await _db.Invoices
            .Where(i => i.SchoolYearId == activeYearId)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.Student).ThenInclude(s => s.Person)
            .ToListAsync();

        var totalBilled = invoices.Sum(i => i.Lines.Sum(l => l.Amount));
        var totalCollected = invoices.Sum(i => i.Payments.Sum(p => p.Amount));
        var outstanding = totalBilled - totalCollected;
        var openInvoicesCount = invoices.Count(i => i.Balance > 0);
        var overdueInvoices = invoices.Where(i => i.Balance > 0 && i.IssuedDate < DateTime.Today.AddDays(-30)).ToList(); // 30 days overdue
        var overdueCount = overdueInvoices.Count;

        // Expenses for active year (assuming expenses have Date within school year range)
        var expenses = await _db.Expenses
            .Where(e => activeYear == null || (e.Date >= activeYear.StartDate && e.Date <= activeYear.EndDate))
            .OrderByDescending(e => e.Date)
            .ToListAsync();
        var totalExpenses = expenses.Sum(e => e.Amount);
        var netPosition = totalCollected - totalExpenses;

        // Breakdown: tuition vs other fees collected (simple heuristic: lines with description containing "tuition")
        var tuitionCollected = invoices
            .SelectMany(i => i.Payments)
            .Where(p => p.Invoice.Lines.Any(l => l.Description.ToLower().Contains("tuition")))
            .Sum(p => p.Amount);
        var otherFeesCollected = totalCollected - tuitionCollected;

        // Recent payments (last 10 across active year)
        var recentPayments = await _db.Payments
            .Include(p => p.Invoice).ThenInclude(i => i.Student).ThenInclude(s => s.Person)
            .Include(p => p.Invoice).ThenInclude(i => i.SchoolYear)
            .Where(p => p.Invoice.SchoolYearId == activeYearId)
            .OrderByDescending(p => p.Date)
            .Take(10)
            .ToListAsync();

        // Recent expenses (last 10)
        var recentExpenses = expenses.Take(10).ToList();

        var vm = new FinanceDashboardVM
        {
            TotalBilled = totalBilled,
            TotalCollected = totalCollected,
            OutstandingReceivables = outstanding,
            OpenInvoicesCount = openInvoicesCount,
            OverdueInvoicesCount = overdueCount,
            TotalExpenses = totalExpenses,
            NetPosition = netPosition,
            TuitionCollected = tuitionCollected,
            OtherFeesCollected = otherFeesCollected,
            RecentPayments = recentPayments,
            OverdueInvoices = overdueInvoices,
            RecentExpenses = recentExpenses,
            ActiveSchoolYearName = activeYear?.Name ?? "\u2014"
        };

        return View(vm);
    }
}