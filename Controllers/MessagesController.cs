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

/// <summary>Module 7 — Parent/Student CRM + staff messaging. Recipient rules:
/// Admin → Admin/Registrar/Faculty/Finance; Finance → Finance/Admin/Registrar;
/// Registrar → Registrar/Finance/Parent — all plain direct threads (no student
/// attached). Faculty talks to the parent of a student in their advised sections;
/// Parent to their child's adviser — both student-scoped threads.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Faculty},{AppRoles.Parent},{AppRoles.Finance},{AppRoles.Registrar}")]
public class MessagesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public MessagesController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // Role-based recipient rules for staff senders. Faculty/Parent use the
    // student-scoped flow instead (no role list).
    private static readonly Dictionary<string, string[]> StaffRecipientRoles = new()
    {
        [AppRoles.Admin] = new[] { AppRoles.Admin, AppRoles.Registrar, AppRoles.Faculty, AppRoles.Finance },
        [AppRoles.Finance] = new[] { AppRoles.Finance, AppRoles.Admin, AppRoles.Registrar },
        [AppRoles.Registrar] = new[] { AppRoles.Registrar, AppRoles.Finance, AppRoles.Parent },
    };

    private static readonly string[] RolePriority =
        { AppRoles.Admin, AppRoles.Registrar, AppRoles.Faculty, AppRoles.Finance, AppRoles.Parent };

    private string MyRole()
    {
        foreach (var r in RolePriority)
            if (User.IsInRole(r)) return r;
        return "";
    }

    private bool IsStaff =>
        User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Finance) || User.IsInRole(AppRoles.Registrar);

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

    // Users the current staff user may message: anyone holding one of their allowed roles.
    private async Task<List<ApplicationUser>> AllowedRecipientsAsync()
    {
        var roles = StaffRecipientRoles[MyRole()];
        var ids = await _db.UserRoles
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name != null && roles.Contains(x.Name))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();
        return await _users.Users
            .Include(u => u.Person)
            .Where(u => ids.Contains(u.Id))
            .OrderBy(u => u.Person!.LastName)
            .ThenBy(u => u.Person!.FirstName)
            .ToListAsync();
    }

    private async Task<bool> RecipientAllowedAsync(string[] roles, string recipientId) =>
        await _db.UserRoles
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .AnyAsync(x => x.UserId == recipientId && x.Name != null && roles.Contains(x.Name));

    // The child's adviser's linked login (Parent → adviser direction). Select-only —
    // EF translates the navigation joins; no Include needed.
    private async Task<string?> AdviserUserIdAsync(int? sectionId) =>
        await _db.Sections
            .Where(s => s.Id == sectionId)
            .Select(s => s.Adviser == null ? null : s.Adviser.Person!.User!.Id)
            .FirstOrDefaultAsync();

    public async Task<IActionResult> Index(string? q, string? direction, string? read, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        var meId = _users.GetUserId(User)!;
        var students = await MyStudentsAsync();
        var studentIds = students.Select(s => s.Id).ToHashSet();

        // Inbox: my direct threads (StudentId null) plus student-scoped threads I'm part of.
        var msgs = await _db.Messages
            .Where(m => (m.SenderId == meId || m.RecipientId == meId)
                && (m.StudentId == null || studentIds.Contains(m.StudentId.Value)))
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
                m.Body.ToLower().Contains(term) || (m.StudentId != null && matchingStudentIds.Contains(m.StudentId.Value)))
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
            // Every role may compose; Faculty/Parent need students in their care to send.
            CanCompose = true
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? studentId)
    {
        if (IsStaff)
        {
            var recipients = await AllowedRecipientsAsync();
            ViewBag.Recipients = new SelectList(recipients.Select(u => new
            {
                Id = u.Id,
                Label = $"{(u.Person?.FullName ?? u.Email ?? "")} ({u.Email})"
            }), "Id", "Label");
            return View();
        }

        var students = await MyStudentsAsync();
        ViewBag.Students = new SelectList(students, "Id", "FullName", studentId);
        ViewBag.IsFaculty = User.IsInRole(AppRoles.Faculty);
        ViewBag.IsParent = User.IsInRole(AppRoles.Parent);
        ViewBag.HasStudents = students.Count > 0;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? studentId, string? recipientId, string body)
    {
        body = body?.Trim() ?? "";
        if (string.IsNullOrEmpty(body))
        {
            ModelState.AddModelError("", "Message cannot be empty.");
            return await ComposeViewAsync(studentId);
        }

        string? toId;
        int? aboutId = null;

        if (IsStaff)
        {
            // Staff senders: plain direct thread (no student), recipient per role rules.
            var roles = StaffRecipientRoles[MyRole()];
            if (string.IsNullOrEmpty(recipientId) || !await RecipientAllowedAsync(roles, recipientId))
            {
                ModelState.AddModelError("", "Pick a valid recipient.");
                return await ComposeViewAsync(studentId);
            }
            toId = recipientId;
        }
        else
        {
            // Faculty/Parent — student-scoped: resolve the student from the caller's scoped
            // set so they cannot compose a message about a student outside their care.
            var allowed = await MyStudentsAsync();
            var student = allowed.FirstOrDefault(s => s.Id == studentId);
            if (student is null)
            {
                ModelState.AddModelError("", "Student not found or not in your care.");
                return await ComposeViewAsync(studentId);
            }

            if (User.IsInRole(AppRoles.Faculty))
                toId = student.Person?.User?.Id; // the child's linked parent
            else // Parent → their child's adviser
                toId = await AdviserUserIdAsync(student.SectionId);
            aboutId = student.Id;
        }

        if (string.IsNullOrEmpty(toId))
        {
            ModelState.AddModelError("", "No linked recipient for this student yet (no Parent login, or no adviser assigned).");
            return await ComposeViewAsync(studentId);
        }

        _db.Messages.Add(new Message
        {
            SenderId = _users.GetUserId(User)!,
            RecipientId = toId,
            StudentId = aboutId,
            Body = body,
            SentAt = DateTime.UtcNow,
            Seen = false
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Conversation view for one thread: student-scoped (Faculty/Parent, plus staff
    // participants viewing) or a plain staff thread (with = the other party's user id).
    [HttpGet]
    public async Task<IActionResult> Thread(int? studentId, string? with)
    {
        var meId = _users.GetUserId(User);
        Student? student = null;
        List<Message> msgs;

        if (studentId is not null)
        {
            // Student thread — I must be in the student's scoped set or a thread participant.
            var students = await MyStudentsAsync();
            student = students.FirstOrDefault(s => s.Id == studentId);
            var participant = student is not null || await _db.Messages
                .AnyAsync(m => m.StudentId == studentId && (m.SenderId == meId || m.RecipientId == meId));
            if (!participant) return NotFound();

            msgs = await _db.Messages
                .Where(m => m.StudentId == studentId
                    && (m.SenderId == meId || m.RecipientId == meId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
        else if (!string.IsNullOrEmpty(with) && with != meId)
        {
            // Staff thread — messages between me and `with` with no student attached.
            msgs = await _db.Messages
                .Where(m => m.StudentId == null
                    && (m.SenderId == meId || m.RecipientId == meId)
                    && (m.SenderId == with || m.RecipientId == with))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
        else return NotFound();

        ViewBag.Student = student;
        ViewBag.UserId = meId;
        ViewBag.WithId = with;
        // Other party: explicit for staff threads, otherwise derived from the thread itself.
        var otherId = !string.IsNullOrEmpty(with)
            ? with
            : msgs.FirstOrDefault(m => m.RecipientId == meId)?.SenderId
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
    public async Task<IActionResult> Thread(int? studentId, string? with, string body)
    {
        body = body?.Trim() ?? "";
        var meId = _users.GetUserId(User);

        string? toId;
        int? aboutId = null;

        if (studentId is not null)
        {
            // Student thread reply: resolve from my scoped set; recipient per direction
            // (Faculty → child's parent, Parent → child's adviser).
            var allowed = await MyStudentsAsync();
            var student = allowed.FirstOrDefault(s => s.Id == studentId);
            if (student is null) return NotFound();

            if (User.IsInRole(AppRoles.Faculty))
                toId = student.Person?.User?.Id;
            else
                toId = await AdviserUserIdAsync(student.SectionId);
            aboutId = studentId;
        }
        else if (!string.IsNullOrEmpty(with) && with != meId)
        {
            // Staff thread reply — recipient must satisfy my role rules.
            var roles = StaffRecipientRoles.TryGetValue(MyRole(), out var rr) ? rr : Array.Empty<string>();
            if (!await RecipientAllowedAsync(roles, with))
            {
                ModelState.AddModelError("", "You cannot message this recipient.");
                return await Thread(null, with);
            }
            toId = with;
        }
        else return NotFound();

        if (string.IsNullOrEmpty(toId))
        {
            ModelState.AddModelError("", "No linked recipient for this student yet.");
            return await Thread(studentId, with);
        }

        if (string.IsNullOrEmpty(body))
        {
            ModelState.AddModelError("", "Message cannot be empty.");
            return await Thread(studentId, with);
        }

        _db.Messages.Add(new Message
        {
            SenderId = meId!,
            RecipientId = toId,
            StudentId = aboutId,
            Body = body,
            SentAt = DateTime.UtcNow,
            Seen = false
        });
        await _db.SaveChangesAsync();
        return studentId is not null
            ? RedirectToAction(nameof(Thread), new { studentId })
            : RedirectToAction(nameof(Thread), new { with });
    }

    private async Task<IActionResult> ComposeViewAsync(int? studentId = null)
    {
        if (IsStaff)
        {
            var recipients = await AllowedRecipientsAsync();
            ViewBag.Recipients = new SelectList(recipients.Select(u => new
            {
                Id = u.Id,
                Label = $"{(u.Person?.FullName ?? u.Email ?? "")} ({u.Email})"
            }), "Id", "Label");
            return View();
        }

        var students = await MyStudentsAsync();
        ViewBag.Students = new SelectList(students, "Id", "FullName", studentId);
        ViewBag.IsFaculty = User.IsInRole(AppRoles.Faculty);
        ViewBag.IsParent = User.IsInRole(AppRoles.Parent);
        ViewBag.HasStudents = students.Count > 0;
        return View();
    }
}
