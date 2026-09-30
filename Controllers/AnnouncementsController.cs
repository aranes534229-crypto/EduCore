using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduCore.ViewModels;

namespace EduCore.Controllers;

/// <summary>Module 7 — Announcements. Everyone but Finance views them; Admin/Registrar/Faculty post.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar},{AppRoles.Faculty},{AppRoles.Parent}")]
public class AnnouncementsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public AnnouncementsController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        // Base query
        var query = _db.Announcements.AsQueryable();

        // Search filter (Title / Body / AuthorName are all mapped columns)
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(term) ||
                a.Body.ToLower().Contains(term) ||
                a.AuthorName.ToLower().Contains(term));
        }

        // Order
        query = query.OrderByDescending(a => a.PostedAt);

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

        var vm = new AnnouncementsIndexVM
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            CanPost = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Registrar) || User.IsInRole(AppRoles.Faculty)
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar},{AppRoles.Faculty}")]
    public async Task<IActionResult> Create(string title, string body)
    {
        var me = await _users.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == _users.GetUserId(User));
        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(body))
        {
            _db.Announcements.Add(new Announcement
            {
                Title = title.Trim(),
                Body = body.Trim(),
                AuthorName = me?.Person?.FullName ?? me?.Email ?? "—",
                PostedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}