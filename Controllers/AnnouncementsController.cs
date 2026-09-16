using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IActionResult> Index()
    {
        var list = await _db.Announcements.OrderByDescending(a => a.PostedAt).ToListAsync();
        ViewBag.CanPost = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Registrar) || User.IsInRole(AppRoles.Faculty);
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar},{AppRoles.Faculty}")]
    public async Task<IActionResult> Create(string title, string body)
    {
        var me = await _users.GetUserAsync(User);
        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(body))
        {
            _db.Announcements.Add(new Announcement
            {
                Title = title.Trim(),
                Body = body.Trim(),
                AuthorName = me?.DisplayName ?? "—",
                PostedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}