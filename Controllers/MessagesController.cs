using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EduCore.ViewModels;

namespace EduCore.Controllers;

/// <summary>Module 7 — Parent/Student CRM. Messages are scoped by the child: Faculty talk to the
/// parent of a student in their advised section; Parent talks to their own child's adviser.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Faculty},{AppRoles.Parent},{AppRoles.Finance}")]
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
        var meId = _users.GetUserId(User);
        if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Finance))
            return await _db.Students
                .Include(s => s.Person)
                .OrderBy(s => s.Person!.LastName)
                .ThenBy(s => s.Person!.FirstName)
                .ToListAsync();

        if (User.IsInRole(AppRoles.Faculty))
        {
            var facultyIds = await _db.Faculty
                .Include(f => f.Person)
                .Where(f => f.Person!.User!.Id == meId)
                .Select(f => f.Id)
                .ToListAsync();
            return await _db.Students
                .Include(s => s.Person)
                .Where(s => s.Section != null && s.Section.AdviserId != null && facultyIds.Contains(s.Section.AdviserId.Value))
                .OrderBy(s => s.Person!.LastName)
                .ThenBy(s => s.Person!.FirstName)
                .ToListAsync();
        }

        // Parent
        return await _db.Students
            .Include(s => s.Person)
            .Where(s => s.Person!.User!.Id == meId)
            .OrderBy(s => s.Person!.LastName)
            .ThenBy(s => s.Person!.FirstName)
            .ToListAsync();
    }

    public async Task<IActionResult> Index(string? q, string? direction, string? read, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        var meId = _users.GetUserId(User)!;
        var students = await MyStudentsAsync();
        var studentIds = students.Select(s => s.Id).ToHashSet();

        var msgs = await _db.Messages
            .Where(m => (m.SenderId == meId || m.RecipientId == meId) && studentIds.Contains(m.StudentId))
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        // Preserve mark-as-seen flow: mark ALL received unseen first, then filter/page
        // in memory so the "New" badge logic stays identical.
        var receivedUnseen = msgs.Where(m => m.RecipientId == meId && !m.Seen).ToList();
        foreach (var m in receivedUnseen) m.Seen = true;
        if (receivedUnseen.Count > 0) await _db.SaveChangesAsync();

        var userIds = msgs.Select(m => m.SenderId).Concat(msgs.Select(m => m.RecipientId)).Distinct().ToList();
        var nameById = (await _users.Users
            .Include(u => u.Person)
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync())
            .ToDictionary(u => u.Id, u => u.Person?.FullName ?? u.Email ?? "");

        // Search filter — FullName is computed, so match student names from the scoped
        // set in memory, then keep messages about them or containing the term.
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            var matchingStudentIds = students
                .Where(s => s.FullName.ToLower().Contains(term) || s.StudentNumber.ToLower().Contains(term))
                .Select(s => s.Id)
                .ToHashSet();
            msgs = msgs.Where(m =>
                m.Body.ToLower().Contains(term) || matchingStudentIds.Contains(m.StudentId))
                .ToList();
        }

        // Direction filter (Received / Sent)
        if (direction == "received") msgs = msgs.Where(m => m.RecipientId == meId).ToList();
        else if (direction == "sent") msgs = msgs.Where(m => m.SenderId == meId).ToList();

        // Read filter — narrows received messages by Seen state
        if (read == "unread") msgs = msgs.Where(m => m.RecipientId == meId && !m.Seen).ToList();
        else if (read == "read") msgs = msgs.Where(m => m.RecipientId == meId && m.Seen).ToList();

        // Total count before paging
        var totalCount = msgs.Count;

        // Clamp page
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        if (page < 1) page = 1;
        if (totalPages > 0 && page > totalPages) page = totalPages;

        // Paged items
        var items = msgs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var vm = new MessagesIndexVM
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            Direction = direction,
            Read = read,
            UserId = meId,
            Names = nameById,
            Students = students.ToDictionary(s => s.Id),
            UnreadIds = receivedUnseen.Select(m => m.Id).ToHashSet(),
            // Finance reads the CRM but does not compose.
            CanCompose = !User.IsInRole(AppRoles.Finance) && students.Count > 0
        };
        return View(vm);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Faculty},{AppRoles.Parent}")]
    public async Task<IActionResult> Create(int? studentId)
    {
        var students = await MyStudentsAsync();
        if (students.Count == 0) return RedirectToAction(nameof(Index));
        ViewBag.Students = new SelectList(students, "Id", "FullName", studentId);
        ViewBag.IsFaculty = User.IsInRole(AppRoles.Faculty);
        ViewBag.IsParent = User.IsInRole(AppRoles.Parent);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Faculty},{AppRoles.Parent}")]
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
            recipientId = student.Person?.User?.Id; // the child's linked parent (or self for Admin)
        else // Parent → their child's adviser
            recipientId = await _db.Sections
                .Where(s => s.Id == student.SectionId)
                .Include(s => s.Adviser).ThenInclude(a => a!.Person)
                .Select(s => s.Adviser == null ? null : s.Adviser.Person!.User!.Id)
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

    // Parent-only conversation view for one student: the messages to/from the other party
    // (Faculty or Parent) grouped as a thread. Scope via MyStudentsAsync so a parent can
    // only thread their own child.
    [HttpGet]
    [Authorize(Roles = AppRoles.Parent)]
    public async Task<IActionResult> Thread(int studentId)
    {
        var meId = _users.GetUserId(User);
        var students = await MyStudentsAsync();
        var student = students.FirstOrDefault(s => s.Id == studentId);
        if (student is null) return NotFound();

        var msgs = await _db.Messages
            .Where(m => m.StudentId == studentId
                && (m.SenderId == meId || m.RecipientId == meId))
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        ViewBag.Student = student;
        ViewBag.UserId = meId;
        // Other party = the person I'm NOT; for a parent that's the child's adviser.
        var otherId = msgs.FirstOrDefault(m => m.RecipientId == meId)?.SenderId
                      ?? msgs.FirstOrDefault(m => m.SenderId == meId)?.RecipientId;
        if (otherId is not null)
        {
            var otherUser = await _users.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == otherId);
            otherId = otherUser?.Person?.FullName ?? otherUser?.Email ?? otherId;
        }
        ViewBag.OtherParty = otherId ?? "?";
        return View(msgs);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Parent)]
    public async Task<IActionResult> Thread(int studentId, string body)
    {
        body = body?.Trim() ?? "";
        var meId = _users.GetUserId(User);

        // Reuse the parent-scoped student check + adviser resolution from Create, but inline
        // (Create POST is shared with Faculty/Admin and resolves the other direction).
        var allowed = await MyStudentsAsync();
        var student = allowed.FirstOrDefault(s => s.Id == studentId);
        if (student is null) return NotFound();

        var recipientId = await _db.Sections
            .Where(s => s.Id == student.SectionId)
            .Include(s => s.Adviser).ThenInclude(a => a!.Person)
            .Select(s => s.Adviser == null ? null : s.Adviser.Person!.User!.Id)
            .FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(recipientId))
        {
            ModelState.AddModelError("", "No linked recipient for this student yet.");
            return await Thread(studentId);
        }

        if (string.IsNullOrEmpty(body))
        {
            ModelState.AddModelError("", "Message cannot be empty.");
            return await Thread(studentId);
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
        // Mark the recipient's copy unread (seen=false on the new row) — handled by Seen=false above.
        return RedirectToAction(nameof(Thread), new { studentId });
    }

    private async Task<IActionResult> ComposeViewAsync(int? studentId = null)
    {
        var students = await MyStudentsAsync();
        ViewBag.Students = new SelectList(students, "Id", "FullName", studentId);
        ViewBag.IsFaculty = User.IsInRole(AppRoles.Faculty);
        ViewBag.IsParent = User.IsInRole(AppRoles.Parent);
        return View();
    }
}