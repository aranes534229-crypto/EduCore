using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Module 8 — Inquiry &amp; Admission. The Registrar logs lead inquiries, assigns a staff
/// owner, and walks them New → InProgress → Closed/Converted. "Converted" precedes the
/// Student/Enrollment screens (the Registrar creates those from the converted lead).</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
public class InquiriesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public InquiriesController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Inquiries
            .Include(i => i.GradeLevel)
            .OrderByDescending(i => i.DateCreated)
            .ToListAsync();
        ViewBag.Assignees = await StaffSelectAsync();
        ViewBag.Names = await NamesOfAsync(list.Where(i => i.AssignedToId != null).Select(i => i.AssignedToId!).Distinct());
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateChoicesAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Inquiry inquiry)
    {
        if (ModelState.IsValid)
        {
            inquiry.Status = InquiryStatus.New;
            _db.Inquiries.Add(inquiry);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PopulateChoicesAsync(inquiry.GradeLevelId);
        return View(inquiry);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, InquiryStatus status)
    {
        var i = await _db.Inquiries.FindAsync(id);
        if (i is not null) { i.Status = status; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, string? assignedToId)
    {
        var i = await _db.Inquiries.FindAsync(id);
        if (i is not null)
        {
            i.AssignedToId = string.IsNullOrEmpty(assignedToId) ? null : assignedToId;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var i = await _db.Inquiries.FindAsync(id);
        if (i is not null) { _db.Inquiries.Remove(i); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    private async Task<Dictionary<string, string>> NamesOfAsync(IEnumerable<string> ids)
        => (await _users.Users.Where(u => ids.Contains(u.Id)).ToListAsync()).ToDictionary(u => u.Id, u => u.DisplayName);

    private async Task<SelectList> StaffSelectAsync(string? selected = null)
    {
        var admin = await _users.GetUsersInRoleAsync(AppRoles.Admin);
        var reg = await _users.GetUsersInRoleAsync(AppRoles.Registrar);
        var staff = admin.Concat(reg).GroupBy(u => u.Id).Select(g => g.First())
            .OrderBy(u => u.DisplayName).ToList();
        return new SelectList(staff, "Id", "DisplayName", selected);
    }

    private async Task PopulateChoicesAsync(int? gradeLevelId = null)
    {
        ViewBag.GradeLevels = new SelectList(
            await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(), "Id", "Name", gradeLevelId);
        ViewBag.Assignees = await StaffSelectAsync();
    }
}