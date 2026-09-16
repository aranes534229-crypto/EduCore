using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Module 6 — Accounting &amp; Expenses. Money out is logged here; money in already
/// lives in Payments (Module 3), so the ledger's net is collected payments minus expenses.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Finance}")]
public class ExpensesController : Controller
{
    private readonly AppDbContext _db;
    public ExpensesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var expenses = await _db.Expenses.OrderByDescending(e => e.Date).ToListAsync();
        var totalExpenses = expenses.Sum(e => e.Amount);
        var collected = await _db.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0m;
        ViewBag.TotalExpenses = totalExpenses;
        ViewBag.Collected = collected;
        ViewBag.Net = collected - totalExpenses;
        return View(expenses);
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