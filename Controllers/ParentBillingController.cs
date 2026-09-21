using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Tuition &amp; Billing, parent self-serve: a parent sees the statements for their linked
/// child(ren) and records payments (in full or in parts) against each balance. The staff-side
/// invoice management stays in InvoicesController; this is only the portal view/pay surface.
/// Scoping reuses Student.ApplicationUserId (a Parent login → their child), the same mechanism
/// MessagesController.MyStudentsAsync relies on.</summary>
[Authorize(Roles = AppRoles.Parent)]
public class ParentBillingController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ParentBillingController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var meId = _users.GetUserId(User);
        var studentIds = await _db.Students
            .Where(s => s.ApplicationUserId == meId)
            .Select(s => s.Id)
            .ToListAsync();
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
        // FindAsync won't fill Lines/Payments, so Balance (Total - Paid) would read 0.00
        // and block every payment. Must Include both to compute the real remaining balance.
        // (Same fix as InvoicesController.AddPayment.)
        var inv = await _db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inv is null || !await OwnsAsync(inv.StudentId)) return NotFound();
        if (p.Amount <= 0) return RedirectToAction(nameof(Details), new { id });

        // A parent can only ever pay from their own account — never accept a posted
        // method (a tampered form mustn't record "Manual"). Fixed value, always.
        p.Method = PaymentMethods.ParentOnly;

        // Block overpayment (same rule as InvoicesController.AddPayment) so a parent can't drive
        // Invoice.Balance negative. Pay-in-parts = post partial payments any time until 0.
        if (p.Amount > inv.Balance)
        {
            TempData["Error"] = $"Payment exceeds the remaining balance of {inv.Balance.ToString("N2")}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        p.Id = 0;
        p.InvoiceId = id;
        _db.Payments.Add(p);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    // A parent may only see/pay their own child's statement.
    private async Task<bool> OwnsAsync(int studentId)
    {
        var meId = _users.GetUserId(User);
        return await _db.Students.AnyAsync(s => s.Id == studentId && s.ApplicationUserId == meId);
    }
}