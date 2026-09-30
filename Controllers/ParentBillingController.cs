using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Tuition & Billing, parent self-serve: a parent sees the statements for their linked
/// child(ren) and records payments (in full or in parts) against each balance. The staff-side
/// invoice management stays in InvoicesController; this is only the portal view/pay surface.
/// Scoping uses Person.UserId (a Parent login → their child via Person), the same mechanism
/// MessagesController.MyStudentsAsync relies on.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Parent}")]
public class ParentBillingController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ParentBillingController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    private async Task<List<int>> GetMyStudentIdsAsync(string meId, string? meEmail)
    {
        return await _db.Students
            .Include(s => s.Person)
            .Where(s => (s.Person != null && s.Person.User != null && s.Person.User.Id == meId)
                     || (meEmail != null && s.GuardianEmail != null && s.GuardianEmail.ToLower() == meEmail))
            .Select(s => s.Id)
            .ToListAsync();
    }

    public async Task<IActionResult> Index()
    {
        var meId = _users.GetUserId(User);
        var me = await _users.GetUserAsync(User);
        var meEmail = me?.Email?.ToLower();
        var studentIds = await GetMyStudentIdsAsync(meId, meEmail);
        var list = await _db.Invoices
            .Include(i => i.Student)
            .Include(i => i.SchoolYear)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => studentIds.Contains(i.StudentId))
            .OrderByDescending(i => i.IssuedDate)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var inv = await _db.Invoices
            .Include(i => i.Student)
            .Include(i => i.SchoolYear)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inv is null || !await OwnsAsync(inv.StudentId)) return NotFound();
        return View(inv);
    }

    public async Task<IActionResult> Receipt(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Invoice).ThenInclude(i => i.Student)
            .Include(p => p.Invoice).ThenInclude(i => i.SchoolYear)
            .Include(p => p.Invoice).ThenInclude(i => i.Lines)
            .Include(p => p.Invoice).ThenInclude(i => i.Payments)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payment is null || !await OwnsAsync(payment.Invoice.StudentId)) return NotFound();
        return View("~/Views/Shared/Receipt.cshtml", payment);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, [Bind("Amount,Date,Method,Reference")] Payment p)
    {
        var inv = await _db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inv is null || !await OwnsAsync(inv.StudentId)) return NotFound();
        if (p.Amount <= 0) return RedirectToAction(nameof(Details), new { id });

        p.Method = PaymentMethods.ParentOnly;

        if (p.Amount > inv.Balance)
        {
            TempData["Error"] = $"Payment exceeds the remaining balance of ₱{inv.Balance.ToString("N2")}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        p.Id = 0;
        p.InvoiceId = id;
        _db.Payments.Add(p);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<bool> OwnsAsync(int studentId)
    {
        var meId = _users.GetUserId(User);
        var me = await _users.GetUserAsync(User);
        var meEmail = me?.Email?.ToLower();
        return await _db.Students
            .Include(s => s.Person)
            .AnyAsync(s => s.Id == studentId &&
                ((s.Person != null && s.Person.User != null && s.Person.User.Id == meId)
                 || (meEmail != null && s.GuardianEmail != null && s.GuardianEmail.ToLower() == meEmail)));
    }
}