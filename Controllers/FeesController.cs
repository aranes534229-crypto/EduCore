using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Module 3 — master fee list. Admin only; Finance works with the invoices (InvoicesController).</summary>
[Authorize(Roles = AppRoles.Admin)]
public class FeesController : Controller
{
    private readonly AppDbContext _db;
    public FeesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var fees = await _db.Fees
            .Include(f => f.GradeLevel)
            .OrderBy(f => f.GradeLevel != null)
            .ThenBy(f => f.GradeLevel!.SortOrder)
            .ThenBy(f => f.Name)
            .ToListAsync();
        return View(fees);
    }

    public async Task<IActionResult> Details(int id)
    {
        var f = await _db.Fees
            .Include(f => f.GradeLevel)
            .FirstOrDefaultAsync(f => f.Id == id);
        return f is null ? NotFound() : View(f);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateGradeLevelsAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Fee f)
    {
        if (!ModelState.IsValid)
        {
            await PopulateGradeLevelsAsync(f.GradeLevelId);
            return View(f);
        }
        _db.Fees.Add(f);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var f = await _db.Fees.FindAsync(id);
        if (f is null) return NotFound();
        await PopulateGradeLevelsAsync(f.GradeLevelId);
        return View(f);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Fee f)
    {
        if (id != f.Id) return BadRequest();
        var existing = await _db.Fees.FindAsync(id);
        if (existing is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateGradeLevelsAsync(f.GradeLevelId);
            return View(f);
        }

        existing.Name = f.Name;
        existing.Amount = f.Amount;
        existing.IsActive = f.IsActive;
        existing.GradeLevelId = f.GradeLevelId;
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

    private async Task PopulateGradeLevelsAsync(int? selected = null)
    {
        ViewBag.GradeLevels = new SelectList(
            await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(), "Id", "Name", selected);
    }
}