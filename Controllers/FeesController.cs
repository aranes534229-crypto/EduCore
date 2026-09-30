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

    public async Task<IActionResult> Index(string? q, int? gradeId, bool? isActive, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        // Base query
        var query = _db.Fees
            .Include(f => f.GradeLevel)
            .Include(f => f.Lines)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(f => f.Name.ToLower().Contains(term));
        }

        // Grade level filter
        if (gradeId.HasValue)
        {
            query = query.Where(f => f.GradeLevelId == gradeId.Value);
        }

        // Status filter (Active / Inactive)
        if (isActive.HasValue)
        {
            query = query.Where(f => f.IsActive == isActive.Value);
        }

        // Order — master list reads best grouped by grade, then name
        query = query
            .OrderBy(f => f.GradeLevel != null)
            .ThenBy(f => f.GradeLevel!.SortOrder)
            .ThenBy(f => f.Name);

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

        var vm = new FeesIndexVM
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            GradeLevelId = gradeId,
            IsActive = isActive,
            GradeLevels = new SelectList(
                await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(), "Id", "Name")
        };
        return View(vm);
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
        vm.Items = (vm.Items ?? []).Where(i => !string.IsNullOrWhiteSpace(i.Description)).ToList();

        if (!ModelState.IsValid)
        {
            await PopulateGradeLevelsAsync(vm.Fee.GradeLevelId);
            return View(vm);
        }

        foreach (var item in vm.Items.Where(i => i.IsActive))
        {
            vm.Fee.Lines.Add(new FeeLine
            {
                Description = item.Description!.Trim(),
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

        vm.Items = (vm.Items ?? []).Where(i => i.Id.HasValue || !string.IsNullOrWhiteSpace(i.Description)).ToList();

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
            .Where(i => i.Id.HasValue && i.IsActive && !string.IsNullOrWhiteSpace(i.Description))
            .Select(i => i.Id!.Value)
            .ToHashSet();

        foreach (var item in vm.Items.Where(i => i.Id.HasValue))
        {
            var line = existing.Lines.FirstOrDefault(l => l.Id == item.Id!.Value);
            if (line is not null)
            {
                if (string.IsNullOrWhiteSpace(item.Description))
                {
                    line.IsActive = false;
                }
                else
                {
                    line.Description = item.Description.Trim();
                    line.Amount = item.Amount;
                    line.IsActive = item.IsActive;
                }
            }
        }

        // Insert new line items (no Id = new).
        foreach (var item in vm.Items.Where(i => !i.Id.HasValue && i.IsActive && !string.IsNullOrWhiteSpace(i.Description)))
        {
            existing.Lines.Add(new FeeLine
            {
                FeeId = existing.Id,
                Description = item.Description!.Trim(),
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
