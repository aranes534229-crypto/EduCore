using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Module 3 — master fee list. Admin only; Finance works with the invoices (InvoicesController).</summary>
[Authorize(Roles = AppRoles.Admin)]
public class FeesController : Controller
{
    private readonly AppDbContext _db;
    public FeesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Fees.OrderBy(f => f.Name).ToListAsync());

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Fee f)
    {
        if (!ModelState.IsValid) return View(f);
        _db.Fees.Add(f);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var f = await _db.Fees.FindAsync(id);
        if (f is not null)
        {
            // Safe to remove: invoice lines snapshot the fee, so no FK points back at it.
            _db.Fees.Remove(f);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}