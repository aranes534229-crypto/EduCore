using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Module 3 — Tuition &amp; Billing. Admin/Finance generate an invoice from the fee list,
/// then record payments against it; the balance is computed from Total minus Paid.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Finance}")]
public class InvoicesController : Controller
{
    private readonly AppDbContext _db;
    public InvoicesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Invoices
            .Include(i => i.Student)
            .Include(i => i.SchoolYear)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .OrderByDescending(i => i.Number)
            .ToListAsync();

        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateChoicesAsync();
        ViewBag.Fees = await _db.Fees.Where(f => f.IsActive).OrderBy(f => f.Name).ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int studentId, int schoolYearId, int[]? feeIds)
    {
        if (await _db.Invoices.AnyAsync(i => i.StudentId == studentId && i.SchoolYearId == schoolYearId))
            ModelState.AddModelError("", "This student already has an invoice for that school year.");

        var fees = feeIds is null
            ? new List<Fee>()
            : await _db.Fees.Where(f => f.IsActive && feeIds.Contains(f.Id)).ToListAsync();
        if (fees.Count == 0)
            ModelState.AddModelError("", "Select at least one fee.");

        if (!ModelState.IsValid)
        {
            await PopulateChoicesAsync(studentId, schoolYearId);
            ViewBag.Fees = await _db.Fees.Where(f => f.IsActive).OrderBy(f => f.Name).ToListAsync();
            return View();
        }

        var invoice = new Invoice { StudentId = studentId, SchoolYearId = schoolYearId };
        invoice.Number = await InvoiceNumbering.NextAsync(_db);
        invoice.Lines = fees.SelectMany(f =>
    f.Lines.Where(fl => fl.IsActive)
        .Select(fl => new InvoiceLine { Description = fl.Description, Amount = fl.Amount }))
    .ToList();
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var inv = await _db.Invoices
            .Include(i => i.Student)
            .Include(i => i.SchoolYear)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        return inv is null ? NotFound() : View(inv);
    }

    public async Task<IActionResult> Receipt(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Invoice).ThenInclude(i => i.Student)
            .Include(p => p.Invoice).ThenInclude(i => i.SchoolYear)
            .Include(p => p.Invoice).ThenInclude(i => i.Lines)
            .Include(p => p.Invoice).ThenInclude(i => i.Payments) // so Balance reflects all payments
            .FirstOrDefaultAsync(p => p.Id == id);
        return payment is null ? NotFound() : View("~/Views/Shared/Receipt.cshtml", payment);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPayment(int id, [Bind("Amount,Date,Method,Reference")] Payment p)
    {
        // FindAsync won't fill Lines/Payments, so Balance (Total - Paid) would read 0.00
        // and block every payment. Must Include both to compute the real remaining balance.
        var inv = await _db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inv is null || p.Amount <= 0) return RedirectToAction(nameof(Details), new { id });

        // Block overpayment: a payment can't exceed the remaining balance, so
        // Invoice.Balance (Total - Paid) can never go negative.
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var inv = await _db.Invoices.FindAsync(id);
        if (inv is not null)
        {
            // Payments and lines cascade with the invoice.
            _db.Invoices.Remove(inv);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateChoicesAsync(int? studentId = null, int? schoolYearId = null)
    {
        ViewBag.Students = new SelectList(
            await _db.Students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync(),
            "Id", "FullName", studentId);
        ViewBag.SchoolYears = new SelectList(
            await _db.SchoolYears.OrderByDescending(y => y.StartDate).ToListAsync(),
            "Id", "Name", schoolYearId);
    }
}