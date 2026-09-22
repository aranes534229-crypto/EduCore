using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Module 8 — Inquiry &amp; Admission. The Registrar logs lead inquiries, assigns a staff
/// owner, and walks them New → Approved/Rejected. "Approved" precedes the
/// Student/Enrollment screens (the Registrar creates those from the approved lead).
/// Parents/Students can also submit inquiries from the portal; those are pre-linked to the
/// user's account and appear in the Registrar's list for follow-up.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar},{AppRoles.Parent}")]
public class InquiriesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IWebHostEnvironment _env;

    public InquiriesController(AppDbContext db, UserManager<ApplicationUser> users, IWebHostEnvironment env)
    {
        _db = db;
        _users = users;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Inquiries
            .Include(i => i.GradeLevel)
            .Include(i => i.CreatedBy)
            .Include(i => i.Thread)
            .OrderByDescending(i => i.Id)
            .ToListAsync();
        ViewBag.Names = await NamesOfAsync(list.Where(i => i.AssignedToId != null).Select(i => i.AssignedToId!).Distinct());
        return View(list);
    }

        /// <summary>Parent/Student: list of inquiries submitted by the logged-in user.</summary>
        [Authorize(Roles = AppRoles.Parent)]
        public async Task<IActionResult> MyInquiries()
        {
            var userId = _users.GetUserId(User);
            var list = await _db.Inquiries
                .Include(i => i.GradeLevel)
                .Include(i => i.Assignee)
                .Include(i => i.Thread)
                    .ThenInclude(n => n.Staff)
                .Where(i => i.CreatedByUserId == userId)
                .OrderByDescending(i => i.DateCreated)
                .ToListAsync();
            return View(list);
        }

        /// <summary>Parent/Student: view an inquiry they own, with the conversation thread.</summary>
        [Authorize(Roles = AppRoles.Parent)]
        public async Task<IActionResult> ParentDetails(int id)
        {
            var userId = _users.GetUserId(User);
            var inquiry = await _db.Inquiries
                .Include(i => i.GradeLevel)
                .Include(i => i.Assignee)
                .Include(i => i.Thread)
                    .ThenInclude(n => n.Staff)
                .FirstOrDefaultAsync(i => i.Id == id && i.CreatedByUserId == userId);

            if (inquiry is null) return NotFound();
            return View(inquiry);
        }

    /// <summary>Registrar/Admin: view one inquiry with the full conversation thread.</summary>
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
        public async Task<IActionResult> Details(int id)
        {
            var inquiry = await _db.Inquiries
                .Include(i => i.GradeLevel)
                .Include(i => i.Assignee)
                .Include(i => i.Documents)
                .Include(i => i.ConvertedStudent)
                .Include(i => i.Thread)
                    .ThenInclude(n => n.Staff)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inquiry is null) return NotFound();

            ViewBag.Names = inquiry.AssignedToId != null
                ? await NamesOfAsync(new[] { inquiry.AssignedToId! })
                : new Dictionary<string, string>();
            return View(inquiry);
        }

    /// <summary>Parent/Student: submit a new inquiry, auto-linked to their account. The single form
    /// captures the student info, the enrollment application fields, and document uploads.</summary>
    [Authorize(Roles = AppRoles.Parent)]
    public async Task<IActionResult> Submit()
    {
        await PopulateChoicesAsync();
        return View();
    }

    [Authorize(Roles = AppRoles.Parent)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Inquiry inquiry, IFormFile[]? uploads)
    {
        var user = await _users.GetUserAsync(User);

        if (!ModelState.IsValid)
        {
            await PopulateChoicesAsync(inquiry.GradeLevelId);
            return View(inquiry);
        }

        inquiry.Status = InquiryStatus.New;
        inquiry.CreatedByUserId = _users.GetUserId(User);
        inquiry.ContactEmail = user?.Email ?? inquiry.ContactEmail;
        inquiry.InquiryNumber = await NextInquiryNumberAsync();
        _db.Inquiries.Add(inquiry);
        await _db.SaveChangesAsync();
        await SaveUploadsAsync(inquiry, uploads);

        TempData["Info"] = "Your inquiry has been submitted. A registrar will follow up soon.";
        return RedirectToAction(nameof(MyInquiries));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, InquiryStatus status)
    {
        var i = await _db.Inquiries.FindAsync(id);
        if (i is not null) { i.Status = status; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var inquiry = await _db.Inquiries.FindAsync(id);
        if (inquiry is null) return NotFound();
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required to reject an inquiry.";
            return RedirectToAction(nameof(Index));
        }

        inquiry.Status = InquiryStatus.Rejected;
        inquiry.AssignedToId = _users.GetUserId(User);
        _db.InquiryNotes.Add(new InquiryNote
        {
            InquiryId = inquiry.Id,
            StaffId = _users.GetUserId(User),
            Visibility = "Parent",
            Body = $"Inquiry rejected: {reason}",
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Info"] = "Inquiry rejected.";
        return RedirectToAction(nameof(Index));
    }

    // ponytail: a converted inquiry's documents were moved onto the Student, so deleting it here
    // only drops the lead record + link, not the Student/Enrollment. Admin-only per spec.
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var i = await _db.Inquiries.FindAsync(id);
        if (i is not null) { _db.Inquiries.Remove(i); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Registrar/Admin: accept the inquiry — creates the Student record (with its admission
    /// ID, name/grade pulled from the inquiry) and a pending enrollment application, moves the parent's
    /// uploaded documents onto the Student, and marks the inquiry Approved.</summary>
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id)
    {
        var inquiry = await _db.Inquiries
            .Include(i => i.GradeLevel)
            .Include(i => i.Documents)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inquiry is null) return NotFound();
        if (inquiry.ConvertedStudentId is not null)
        {
            TempData["Info"] = "This inquiry is already approved.";
            return RedirectToAction(nameof(Index));
        }

        var (first, last) = SplitName(inquiry.StudentName);
        var student = new Student
        {
            StudentNumber = await NextStudentNumberAsync(),
            FirstName = first,
            LastName = last,
            BirthDate = inquiry.BirthDate,
            Address = inquiry.Address,
            GuardianName = inquiry.GuardianName,
            GuardianContact = inquiry.ContactPhone,
            GuardianEmail = inquiry.ContactEmail,
            // An inquiry submitted from the Parent portal carries the submitted account; the
            // converted Student inherits it so the parent sees their application in the portal.
            ApplicationUserId = inquiry.CreatedByUserId
        };
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        var activeYear = await _db.SchoolYears.FirstOrDefaultAsync(y => y.IsActive)
            ?? await _db.SchoolYears.OrderByDescending(y => y.StartDate).FirstOrDefaultAsync();
        Enrollment? enrollment = null;
        if (activeYear is not null)
        {
            enrollment = new Enrollment
            {
                StudentId = student.Id,
                GradeLevelId = inquiry.GradeLevelId,
                SchoolYearId = activeYear.Id,
                Status = EnrollmentStatus.Unscheduled,  // enrolled; waiting on the Registrar to schedule
                Notes = $"Approved from inquiry #{inquiry.InquiryNumber}"
            };
            _db.Enrollments.Add(enrollment);
        }

        foreach (var doc in inquiry.Documents)
        {
            doc.StudentId = student.Id;
            doc.InquiryId = null;
        }

        inquiry.ConvertedStudentId = student.Id;
        inquiry.Status = InquiryStatus.Approved;
        inquiry.AssignedToId = _users.GetUserId(User);
        _db.InquiryNotes.Add(new InquiryNote
        {
            InquiryId = inquiry.Id,
            StaffId = _users.GetUserId(User),
            Visibility = "Registrar",
            Body = enrollment is null
                ? $"Approved to Student #{student.Id} ({student.StudentNumber})."
                : $"Approved to Student #{student.Id} ({student.StudentNumber}); application for {inquiry.GradeLevel.Name} awaiting approval."
        });
        await _db.SaveChangesAsync();

        if (enrollment is not null)
        {
            inquiry.ConvertedEnrollmentId = enrollment.Id;
            await _db.SaveChangesAsync();
        }

        TempData["Info"] = $"Approved — created Student #{student.StudentNumber}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task SaveUploadsAsync(Inquiry inquiry, IEnumerable<IFormFile>? uploads)
    {
        if (uploads is null) return;
        string[] allowed = { ".jpg", ".jpeg", ".png", ".pdf" };   // birth cert / report card / photo
        foreach (var file in uploads)
        {
            if (file is null || file.Length == 0) continue;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) continue;

            var relDir = $"uploads/{DateTime.Today:yyyy}/{DateTime.Today:MM}";
            var dir = Path.Combine(_env.WebRootPath, relDir);
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid():N}{ext}";   // unique on disk; original name kept for display
            using (var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
                await file.CopyToAsync(fs);

            _db.Documents.Add(new Document
            {
                InquiryId = inquiry.Id,
                FileName = file.FileName,
                Path = $"{relDir}/{fileName}",
                Type = ext,
                UploadedAt = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();
    }

    // ponytail: count-based (a deleted student can reuse a number). Lock per-year sequences in a
    // dedicated table if the admission ID must be collision-proof forever.
    private async Task<string> NextStudentNumberAsync()
    {
        var prefix = $"{DateTime.Today.Year}-";
        var count = await _db.Students.CountAsync(s => s.StudentNumber.StartsWith(prefix));
        return $"{prefix}{count + 1:0000}";
    }

    // Inquiries start at 1001 and increment by 1. Count-based so deleted inquiries don't leave gaps
    // in the visible sequence; collisions are impossible unless someone manually truncates the table.
    private async Task<string> NextInquiryNumberAsync()
    {
        var count = await _db.Inquiries.CountAsync();
        return $"INQ-{1001 + count:D4}";
    }

    private static (string First, string Last) SplitName(string full)
    {
        var parts = full.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => ("", ""),
            1 => (parts[0], ""),
            _ => (string.Join(' ', parts[..^1]), parts[^1])
        };
    }

    private async Task<Dictionary<string, string>> NamesOfAsync(IEnumerable<string> ids)
        => (await _users.Users.Where(u => ids.Contains(u.Id)).ToListAsync()).ToDictionary(u => u.Id, u => u.DisplayName);

    private async Task PopulateChoicesAsync(int? gradeLevelId = null)
    {
        ViewBag.GradeLevels = new SelectList(
            await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(), "Id", "Name", gradeLevelId);
    }
}