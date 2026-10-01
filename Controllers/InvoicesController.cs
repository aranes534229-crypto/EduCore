using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EduCore.ViewModels;

namespace EduCore.Controllers;

/// <summary>Module 3 — Tuition &amp; Billing. Admin/Finance generate an invoice from the fee list,
/// then record payments against it; the balance is computed from Total minus Paid.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Finance}")]
public class InvoicesController : Controller
{
    private readonly AppDbContext _db;
    public InvoicesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, InvoicePaymentStatus? status, int? schoolYearId, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        // Base query — Lines and Payments are needed for Total/Paid/Balance
        var query = _db.Invoices
            .Include(i => i.Student).ThenInclude(s => s.Person)
            .Include(i => i.SchoolYear)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            // FullName is a computed property — not translatable by EF Core, so search
            // the mapped columns (StudentNumber, Person.FirstName/LastName) instead.
            query = query.Where(i =>
                i.Student.StudentNumber.ToLower().Contains(term) ||
                i.Number != null && i.Number.ToLower().Contains(term) ||
                i.Student.Person!.FirstName.ToLower().Contains(term) ||
                i.Student.Person!.LastName.ToLower().Contains(term));
        }

        // Status filter — payment state is derived from the balance.
        // Load to memory first for SQLite compatibility, then filter in-memory.
        List<Invoice> invoices;
        if (status.HasValue)
        {
            // Load all matching invoices with relations, then filter in memory
            invoices = await query.ToListAsync();
            invoices = status.Value switch
            {
                InvoicePaymentStatus.Unpaid => invoices.Where(i => i.Balance == i.Total).ToList(),
                InvoicePaymentStatus.PaidInFull => invoices.Where(i => i.Balance == 0 && i.Total > 0).ToList(),
                _ => invoices.Where(i => i.Balance > 0 && i.Balance < i.Total).ToList(),
            };
        }
        else
        {
            invoices = await query.ToListAsync();
        }

        // School year filter (apply in memory if not already applied in query)
        if (schoolYearId.HasValue)
        {
            invoices = invoices.Where(i => i.SchoolYearId == schoolYearId.Value).ToList();
        }

        // Order
        invoices = invoices.OrderByDescending(i => i.Id).ToList();

        // Total count before paging
        var totalCount = invoices.Count;

        // Clamp page
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        if (page < 1) page = 1;
        if (totalPages > 0 && page > totalPages) page = totalPages;

        // Paged items
        var items = invoices
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var vm = new InvoicesIndexVM
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            Status = status,
            SchoolYearId = schoolYearId,
            SchoolYears = new SelectList(
                await _db.SchoolYears.OrderByDescending(y => y.StartDate).ToListAsync(), "Id", "Name")
        };
        return View(vm);
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
            .Include(i => i.Student).ThenInclude(s => s.Person)
            .Include(i => i.SchoolYear)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        return inv is null ? NotFound() : View(inv);
    }

    public async Task<IActionResult> Receipt(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Invoice).ThenInclude(i => i.Student).ThenInclude(s => s.Person)
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
            TempData["Error"] = $"Payment exceeds the remaining balance of ₱{inv.Balance.ToString("N2")}.";
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