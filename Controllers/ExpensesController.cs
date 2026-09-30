using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EduCore.ViewModels;

namespace EduCore.Controllers;

/// <summary>Module 6 — Accounting &amp; Expenses. Money out is logged here; money in already
/// lives in Payments (Module 3), so the ledger's net is collected payments minus expenses.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Finance}")]
public class ExpensesController : Controller
{
    private readonly AppDbContext _db;
    public ExpensesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, string? category, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        // Base query
        var query = _db.Expenses.AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(e => e.Description.ToLower().Contains(term));
        }

        // Category filter
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(e => e.Category == category);
        }

        // Date range filter (To includes the whole day — Date is date-only in practice)
        if (from.HasValue)
        {
            query = query.Where(e => e.Date >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(e => e.Date < to.Value.AddDays(1));
        }

        // Order
        query = query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id);

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

        // Ledger summary tiles stay global — computed from ALL expenses, not the filtered set.
        // SQLite stores decimal as text and can't SUM() it in SQL; cast through double (REAL) for the aggregate.
        var collected = (decimal?)(double?)await _db.Payments.SumAsync(p => (double?)p.Amount) ?? 0m;
        var totalExpenses = (decimal?)(double?)await _db.Expenses.SumAsync(e => (double?)e.Amount) ?? 0m;

        var vm = new ExpensesIndexVM
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            Category = category,
            From = from,
            To = to,
            Categories = new SelectList(
                await _db.Expenses
                    .Where(e => e.Category != null && e.Category != "")
                    .Select(e => e.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync()),
            Collected = collected,
            TotalExpenses = totalExpenses
        };
        return View(vm);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Expense expense)
    {
        if (ModelState.IsValid)
        {
            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(expense);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is not null) { _db.Expenses.Remove(expense); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}