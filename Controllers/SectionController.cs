using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EduCore.ViewModels;

namespace EduCore.Controllers;

/// <summary>Module 5 — sections + who teaches/attends them. Section setup (create sections,
/// advisers, subject/teacher assignment) is Admin-only; Registrar sees the same controller as
/// "Class Schedule" and may only assign/remove students in a section. Grade/attendance entry
/// lives in GradebookController.</summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Registrar}")]
public class SectionsController : Controller
{
    private readonly AppDbContext _db;

    public SectionsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, int? gradeId, int? schoolYearId, bool? isSubmitted, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        // Base query — Subjects and Students are needed for the per-row counts
        var query = _db.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.Adviser)
            .Include(s => s.Subjects)
            .Include(s => s.Students)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            // Adviser.FullName is a computed property — not translatable by EF Core, so search
            // the mapped columns (Section.Name, Person.FirstName/LastName) instead.
            query = query.Where(s =>
                s.Name.ToLower().Contains(term) ||
                s.Adviser!.Person!.FirstName.ToLower().Contains(term) ||
                s.Adviser!.Person!.LastName.ToLower().Contains(term));
        }

        // Grade level filter
        if (gradeId.HasValue)
        {
            query = query.Where(s => s.GradeLevelId == gradeId.Value);
        }

        // School year filter
        if (schoolYearId.HasValue)
        {
            query = query.Where(s => s.SchoolYearId == schoolYearId.Value);
        }

        // Submitted filter — IsSubmitted is a computed property, so filter the
        // mapped SubmittedToFinanceAt column instead (EF can't translate the former).
        if (isSubmitted.HasValue)
        {
            query = isSubmitted.Value
                ? query.Where(s => s.SubmittedToFinanceAt != null)
                : query.Where(s => s.SubmittedToFinanceAt == null);
        }

        // Order
        query = query
            .OrderBy(s => s.GradeLevel.SortOrder)
            .ThenBy(s => s.Name);

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

        var vm = new SectionsIndexVM
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            GradeLevelId = gradeId,
            SchoolYearId = schoolYearId,
            IsSubmitted = isSubmitted,
            GradeLevels = new SelectList(
                await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync(), "Id", "Name"),
            SchoolYears = new SelectList(
                await _db.SchoolYears.OrderByDescending(y => y.StartDate).ToListAsync(), "Id", "Name")
        };
        return View(vm);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create()
    {
        await PopulateChoicesAsync();
        await PopulateSubjectsAsync();
        return View(new Section());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(Section section, int[]? subjectIds)
    {
        if (!ModelState.IsValid)
        {
            await PopulateChoicesAsync(section.GradeLevelId);
            await PopulateSubjectsAsync();
            return View(section);
        }

        _db.Sections.Add(section);
        await _db.SaveChangesAsync();
        await SaveSectionSubjectsAsync(section.Id, subjectIds);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var section = await LoadSectionForManageAsync(id);
        if (section is null) return NotFound();

        await PopulateEditChoicesAsync(section);
        return View(section);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id, Section section, int[]? subjectIds,
        Dictionary<int, int?>? teacherIds)
    {
        if (id != section.Id) return BadRequest();

        var existing = await LoadSectionForManageAsync(id);
        if (existing is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateEditChoicesAsync(section, subjectIds);
            section.Subjects = existing.Subjects;
            section.Students = existing.Students;
            return View(section);
        }

        existing.Name = section.Name;
        existing.GradeLevelId = section.GradeLevelId;
        existing.SchoolYearId = section.SchoolYearId;
        existing.AdviserId = section.AdviserId;

        // Per-subject teacher (optional); the <option value=""> posts as null / untouched subject absent.
        int? Teacher(int sid) => teacherIds != null && teacherIds.TryGetValue(sid, out var f) ? f : null;

        // Diff the join table: drop unselected, set teacher on kept, add newly selected.
        var wanted = (subjectIds ?? Array.Empty<int>()).Distinct().ToHashSet();
        foreach (var ss in existing.Subjects.ToList())
        {
            if (!wanted.Contains(ss.SubjectId))
                _db.SectionSubjects.Remove(ss);                          // grades on removed subjects cascade
            else
            {
                ss.FacultyId = Teacher(ss.SubjectId);
                wanted.Remove(ss.SubjectId);
            }
        }
        foreach (var add in wanted)
            _db.SectionSubjects.Add(new SectionSubject { SectionId = id, SubjectId = add, FacultyId = Teacher(add) });

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var section = await _db.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.SchoolYear)
            .Include(s => s.Adviser)
            .Include(s => s.Students)
            .Include(s => s.Subjects).ThenInclude(ss => ss.Subject)
            .Include(s => s.Subjects).ThenInclude(ss => ss.Faculty)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (section is null) return NotFound();

        return View(section);   // read-only view (management lives on Edit)
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignStudent(int id, int studentId)
    {
        var student = await _db.Students.FindAsync(studentId);
        var section = await _db.Sections.FindAsync(id);
        // ponytail: once submitted to Finance the roster is billed as-is, so lock it against changes.
        if (student is not null && section is not null && !section.IsSubmitted && student.SectionId is null
            && CanSchedule(student, section))
        {
            student.SectionId = id;
            await SetEnrollmentStatusAsync(student.Id, section, EnrollmentStatus.Scheduled);
            await _db.SaveChangesAsync();
        }
        return Back(id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStudent(int id, int studentId)
    {
        var student = await _db.Students.FindAsync(studentId);
        var section = await _db.Sections.FindAsync(id);
        if (student is not null && section is not null && !section.IsSubmitted && student.SectionId == id)
        {
            student.SectionId = null;
            await SetEnrollmentStatusAsync(student.Id, section, EnrollmentStatus.Unscheduled);
            await _db.SaveChangesAsync();
        }
        return Back(id);
    }

    /// <summary>Placement drives the enrollment lifecycle: placing a student in a section makes it
    /// Scheduled; removing them returns it to Unscheduled.</summary>
    private async Task SetEnrollmentStatusAsync(int studentId, Section section, EnrollmentStatus status)
    {
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.StudentId == studentId
            && e.SchoolYearId == section.SchoolYearId);
        if (enrollment is null)
        {
            enrollment = new Enrollment
            {
                StudentId = studentId,
                SchoolYearId = section.SchoolYearId,
                GradeLevelId = section.GradeLevelId,
                Status = status
            };
            _db.Enrollments.Add(enrollment);
        }
        else
        {
            enrollment.GradeLevelId = section.GradeLevelId;
            enrollment.Status = status;
        }

        // Auto-billing: once a student is Scheduled, their grade's tuition invoice appears
        // automatically, sourced from the per-grade Fee row (Module 3). Runs only on the
        // assign path (Scheduled); guarded so a student is never billed twice for the same
        // school year even if re-assigned to another section.
        if (status is EnrollmentStatus.Scheduled
            && !await _db.Invoices.AnyAsync(i => i.StudentId == studentId && i.SchoolYearId == section.SchoolYearId))
        {
            var tuitionFee = await _db.Fees
                .Include(f => f.Lines)
                .FirstOrDefaultAsync(f => f.IsActive && f.GradeLevelId == section.GradeLevelId);

            List<InvoiceLine> lines;
            if (tuitionFee is not null && tuitionFee.TotalAmount > 0)
            {
                var activeLines = tuitionFee.Lines.Where(l => l.IsActive && l.Amount > 0).ToList();
                lines = activeLines.Any()
                    ? activeLines.Select(l => new InvoiceLine { Description = l.Description, Amount = l.Amount }).ToList()
                    : new List<InvoiceLine> { new InvoiceLine { Description = tuitionFee.Name, Amount = tuitionFee.TotalAmount } };
            }
            else
            {
                var grade = await _db.GradeLevels.FindAsync(section.GradeLevelId);
                var amount = grade?.Amount ?? 0m;
                lines = new List<InvoiceLine> { new InvoiceLine { Description = $"{grade?.Name ?? "Grade"} Tuition", Amount = amount } };
            }

            if (lines.Sum(l => l.Amount) > 0)
            {
                _db.Invoices.Add(new Invoice
                {
                    StudentId = studentId,
                    SchoolYearId = section.SchoolYearId,
                    IssuedDate = DateTime.Today,
                    Number = await InvoiceNumbering.NextAsync(_db),
                    Lines = lines
                });
            }
        }
    }

    /// <summary>Registrar may only place students whose enrollment for the section's grade+year is
    /// Unscheduled (enrolled but not yet placed). Admin keeps full placement control.</summary>
    private bool CanSchedule(Student student, Section section)
    {
        if (User.IsInRole(AppRoles.Admin)) return true;
        return _db.Enrollments.Any(e => e.StudentId == student.Id
            && e.SchoolYearId == section.SchoolYearId
            && e.GradeLevelId == section.GradeLevelId
            && e.Status == EnrollmentStatus.Unscheduled);
    }

    // Redirect back to the page that owns the student roster: Admin edits, Registrar schedules.
    private IActionResult Back(int id) =>
        User.IsInRole(AppRoles.Admin)
            ? RedirectToAction(nameof(Edit), new { id })
            : RedirectToAction(nameof(Schedule), new { id });

    /// <summary>Registrar's scheduling screen: assign unscheduled-enrolled students to the section,
    /// then hand the roster to Finance.</summary>
    public async Task<IActionResult> Schedule(int id)
    {
        var section = await _db.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.SchoolYear)
            .Include(s => s.Students)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (section is null) return NotFound();

        ViewBag.Pool = await _db.Students
            .Where(st => st.SectionId == null)
            .Where(st => _db.Enrollments.Any(e => e.StudentId == st.Id
                && e.SchoolYearId == section.SchoolYearId
                && e.GradeLevelId == section.GradeLevelId
                && e.Status == EnrollmentStatus.Unscheduled))
            .Include(st => st.Person)
            .OrderBy(st => st.Person!.LastName)
            .ThenBy(st => st.Person!.FirstName)
            .ToListAsync();
        return View(section);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitToFinance(int id)
    {
        var section = await _db.Sections.FindAsync(id);
        if (section is not null && !section.IsSubmitted)
            section.SubmittedToFinanceAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Schedule), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var section = await _db.Sections.FindAsync(id);
        if (section is not null)
        {
            _db.Sections.Remove(section);   // students keep their row; SectionId set null
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<Section?> LoadSectionForManageAsync(int id) =>
        await _db.Sections
            .Include(s => s.Subjects).ThenInclude(ss => ss.Faculty)
            .Include(s => s.Students)
            .FirstOrDefaultAsync(s => s.Id == id);

    private async Task PopulateEditChoicesAsync(Section section, int[]? submittedSubjectIds = null)
    {
        await PopulateChoicesAsync(section.GradeLevelId, section.SchoolYearId);
        await PopulateSubjectsAsync(submittedSubjectIds ?? section.Subjects.Select(ss => ss.SubjectId));
        ViewBag.Teachers = await _db.Faculty
            .Where(f => f.IsActive)
            .Include(f => f.Person)
            .OrderBy(f => f.Person!.LastName)
            .ThenBy(f => f.Person!.FirstName)
            .ToListAsync();
        // ponytail: student grade = their section's grade level (no GradeLevelId on Student);
        // assignable = anyone not yet placed in a section.
        ViewBag.Pool = await _db.Students
            .Where(st => st.SectionId == null)
            .Include(st => st.Person)
            .OrderBy(st => st.Person!.LastName)
            .ThenBy(st => st.Person!.FirstName)
            .ToListAsync();
    }

    private async Task PopulateChoicesAsync(int? gradeLevelId = null, int? schoolYearId = null)
    {
        var gradeLevels = await _db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync();
        var activeFees = await _db.Fees
            .Include(f => f.Lines)
            .Where(f => f.IsActive)
            .ToListAsync();

        var feeMap = gradeLevels.ToDictionary(
            g => g.Id.ToString(),
            g => {
                var fee = activeFees.FirstOrDefault(f => f.GradeLevelId == g.Id);
                var amount = fee != null && fee.TotalAmount > 0 ? fee.TotalAmount : g.Amount;
                var name = fee != null ? fee.Name : $"{g.Name} Tuition";
                return new { name, amount, formatted = amount.ToString("N2") };
            }
        );

        ViewBag.GradeLevels = new SelectList(gradeLevels, "Id", "Name", gradeLevelId);
        ViewBag.FeeMapJson = System.Text.Json.JsonSerializer.Serialize(feeMap);
        ViewBag.SchoolYears = new SelectList(
            await _db.SchoolYears.OrderByDescending(y => y.StartDate).ToListAsync(), "Id", "Name", schoolYearId);
        ViewBag.Advisers = new SelectList(
            await _db.Faculty
                .Where(f => f.IsActive)
                .Include(f => f.Person)
                .OrderBy(f => f.Person!.LastName)
                .ThenBy(f => f.Person!.FirstName)
                .ToListAsync(), "Id", "FullName");
    }

    private async Task PopulateSubjectsAsync(IEnumerable<int>? selected = null)
    {
        ViewBag.Subjects = await _db.Subjects.OrderBy(x => x.Name).ToListAsync();
        ViewBag.SelectedSubjectIds = (selected ?? Enumerable.Empty<int>()).ToHashSet();
    }

    private async Task SaveSectionSubjectsAsync(int sectionId, int[]? subjectIds)
    {
        if (subjectIds is null) return;
        foreach (var id in subjectIds.Distinct())
            _db.SectionSubjects.Add(new SectionSubject { SectionId = sectionId, SubjectId = id });
        await _db.SaveChangesAsync();
    }
}