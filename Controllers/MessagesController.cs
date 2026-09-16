using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Module 7 — Parent/Student CRM. Messages are scoped by the child: Faculty talk to the
/// parent of a student in their advised section; Parent talks to their own child's adviser.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Faculty},{AppRoles.Parent}")]
public class MessagesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public MessagesController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // Students the current user may message about: Admin → all; Faculty → their advised sections;
    // Parent → their linked child(ren).
    private async Task<List<Student>> MyStudentsAsync()
    {
        // meId is null only for an unauthenticated caller; the role gates at the top of
        // the controller reject anonymous users, so in practice this is non-null. Each
        // branch below is null-safe regardless (empty result on null id).
        var meId = _users.GetUserId(User);
        if (User.IsInRole(AppRoles.Admin))
            return await _db.Students.OrderBy(s => s.LastName).ToListAsync();

        if (User.IsInRole(AppRoles.Faculty))
        {
            var facultyIds = await _db.Faculty
                .Where(f => f.ApplicationUserId == meId)
                .Select(f => f.Id)
                .ToListAsync();
            return await _db.Students
                .Where(s => s.Section != null && s.Section.AdviserId != null && facultyIds.Contains(s.Section.AdviserId.Value))
                .OrderBy(s => s.LastName)
                .ToListAsync();
        }

        // Parent
        return await _db.Students.Where(s => s.ApplicationUserId == meId).ToListAsync();
    }

    public async Task<IActionResult> Index()
    {
        var meId = _users.GetUserId(User);
        var students = await MyStudentsAsync();
        var studentIds = students.Select(s => s.Id).ToHashSet();

        var msgs = await _db.Messages
            .Where(m => (m.SenderId == meId || m.RecipientId == meId) && studentIds.Contains(m.StudentId))
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        var receivedUnseen = msgs.Where(m => m.RecipientId == meId && !m.Seen).ToList();
        foreach (var m in receivedUnseen) m.Seen = true;
        if (receivedUnseen.Count > 0) await _db.SaveChangesAsync();

        var userIds = msgs.Select(m => m.SenderId).Concat(msgs.Select(m => m.RecipientId)).Distinct().ToList();
        var nameById = (await _users.Users.Where(u => userIds.Contains(u.Id)).ToListAsync())
            .ToDictionary(u => u.Id, u => u.DisplayName);

        ViewBag.UserId = meId;
        ViewBag.Names = nameById;
        ViewBag.Students = students.ToDictionary(s => s.Id);
        ViewBag.UnreadIds = receivedUnseen.Select(m => m.Id).ToHashSet();
        ViewBag.CanCompose = students.Count > 0;
        return View(msgs);
    }

    public async Task<IActionResult> Create(int? studentId)
    {
        var students = await MyStudentsAsync();
        if (students.Count == 0) return RedirectToAction(nameof(Index));
        ViewBag.Students = new SelectList(students.OrderBy(s => s.LastName), "Id", "FullName", studentId);
        ViewBag.IsFaculty = User.IsInRole(AppRoles.Faculty);
        ViewBag.IsParent = User.IsInRole(AppRoles.Parent);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int studentId, string body)
    {
        body = body?.Trim() ?? "";

        // Resolve the student from the caller's scoped set so a Faculty/Parent cannot
        // compose a message about a student outside their care (arbitrary studentId).
        var allowed = await MyStudentsAsync();
        var student = allowed.FirstOrDefault(s => s.Id == studentId);
        if (student is null)
        {
            ModelState.AddModelError("", "Student not found or not in your care.");
            return await ComposeViewAsync(studentId);
        }

        var meId = _users.GetUserId(User);

        string? recipientId;
        if (User.IsInRole(AppRoles.Faculty) || User.IsInRole(AppRoles.Admin))
            recipientId = student.ApplicationUserId; // the child's linked parent (or self for Admin)
        else // Parent → their child's adviser
            recipientId = await _db.Sections
                .Where(s => s.Id == student.SectionId)
                .Select(s => s.Adviser == null ? null : s.Adviser.ApplicationUserId)
                .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(recipientId))
        {
            ModelState.AddModelError("", "No linked recipient for this student yet (no Parent login, or no adviser assigned).");
            return await ComposeViewAsync(studentId);
        }

        _db.Messages.Add(new Message
        {
            SenderId = meId!,
            RecipientId = recipientId!,
            StudentId = studentId,
            Body = body,
            SentAt = DateTime.UtcNow,
            Seen = false
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ComposeViewAsync(int? studentId = null)
    {
        var students = await MyStudentsAsync();
        ViewBag.Students = new SelectList(students.OrderBy(s => s.LastName), "Id", "FullName", studentId);
        ViewBag.IsFaculty = User.IsInRole(AppRoles.Faculty);
        ViewBag.IsParent = User.IsInRole(AppRoles.Parent);
        return View();
    }
}