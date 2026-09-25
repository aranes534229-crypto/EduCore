using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using EduCore.ViewModels;
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
            .Include(f => f.Lines)
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
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == id);
        return f is null ? NotFound() : View(f);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateGradeLevelsAsync();
        var vm = new FeeFormVm { Items = [new()] };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeeFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateGradeLevelsAsync(vm.Fee.GradeLevelId);
            return View(vm);
        }

        foreach (var item in vm.Items.Where(i => i.IsActive))
        {
            vm.Fee.Lines.Add(new FeeLine
            {
                Description = item.Description,
                Amount = item.Amount,
                IsActive = true
            });
        }

        if (!vm.Fee.Lines.Any(l => l.IsActive))
            ModelState.AddModelError("", "Add at least one active line item.");
        else
        {
            _db.Fees.Add(vm.Fee);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateGradeLevelsAsync(vm.Fee.GradeLevelId);
        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var f = await _db.Fees
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (f is null) return NotFound();

        await PopulateGradeLevelsAsync(f.GradeLevelId);

        var vm = new FeeFormVm
        {
            Fee = f,
            Items = f.Lines.Select(l => new FeeLineItemVm
            {
                Id = l.Id,
                Description = l.Description,
                Amount = l.Amount,
                IsActive = l.IsActive
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FeeFormVm vm)
    {
        if (id != vm.Fee.Id) return BadRequest();
        var existing = await _db.Fees
            .Include(f => f.Lines)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (existing is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateGradeLevelsAsync(vm.Fee.GradeLevelId);
            return View(vm);
        }

        // Manual field copy (matches existing pattern; avoids overposting + second round-trip).
        existing.Name = vm.Fee.Name;
        existing.IsActive = vm.Fee.IsActive;
        existing.GradeLevelId = vm.Fee.GradeLevelId;

        // Sync line items: update matched IDs, insert new, soft-delete removed.
        var incomingIds = vm.Items
            .Where(i => i.Id.HasValue && i.IsActive)
            .Select(i => i.Id!.Value)
            .ToHashSet();

        foreach (var item in vm.Items.Where(i => i.Id.HasValue))
        {
            var line = existing.Lines.FirstOrDefault(l => l.Id == item.Id!.Value);
            if (line is not null)
            {
                line.Description = item.Description;
                line.Amount = item.Amount;
                line.IsActive = item.IsActive;
            }
        }

        // Insert new line items (no Id = new).
        foreach (var item in vm.Items.Where(i => !i.Id.HasValue && i.IsActive))
        {
            existing.Lines.Add(new FeeLine
            {
                FeeId = existing.Id,
                Description = item.Description,
                Amount = item.Amount,
                IsActive = true
            });
        }

        // Soft-delete lines not in the active incoming set.
        foreach (var line in existing.Lines.Where(l => l.IsActive && !incomingIds.Contains(l.Id)).ToList())
        {
            line.IsActive = false;
        }

        if (!existing.Lines.Any(l => l.IsActive))
            ModelState.AddModelError("", "Add at least one active line item.");
        else
        {
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateGradeLevelsAsync(vm.Fee.GradeLevelId);
        return View(vm);
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
