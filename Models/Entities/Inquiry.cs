using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduCore.Models.Entities;

/// <summary>A prospective-student lead logged by the Registrar or submitted by a Parent/Student.
/// Lifecycle: New → Approved/Rejected. "Approved" links the lead to the Student + Enrollment
/// records the Registrar created from it (see <see cref="ConvertedEnrollmentId"/>).</summary>
public enum InquiryStatus { New, Approved, Rejected }

/// <summary>Fixed, well-known inquiry channels and categories used for reporting/analytics.
/// Kept as constants (not an enum) so the Registrar can fill in any value, absent a fixed list.</summary>
public static class InquiryDefaults
{
    public static readonly string[] Sources = { "Walk-in", "Website", "Phone", "Referral", "Social media", "Fair" };
    public static readonly string[] Types  = { "Admissions", "Fees", "Requirements", "Schedule", "Other" };
}

public class Inquiry
{
    public int Id { get; set; }

    [Required]
    public string StudentName { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Please select a grade level.")]
    public int GradeLevelId { get; set; }

    /// <summary>Set by EF when loaded — never bound from a form, so validation must skip it
    /// (its FK <see cref="GradeLevelId"/> carries the required/range checks instead). Without
    /// <see cref="ValidateNeverAttribute"/> the required-navigation rule fails every POST.</summary>
    [ValidateNever]
    public GradeLevel GradeLevel { get; set; } = null!;

    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";

    public string? InternalNotes { get; set; }

    // The enrollment-application fields a parent fills in at submit/apply time. They populate the
    // Student record on Convert (see InquiriesController.Convert), so the registrar doesn't retype them.
    public DateTime? BirthDate { get; set; }
    public string Address { get; set; } = "";
    public string GuardianName { get; set; } = "";

    /// <summary>Where the lead came from (see <see cref="InquiryDefaults.Sources"/>).</summary>
    public string Source { get; set; } = InquiryDefaults.Sources[0];

    /// <summary>What the inquiry is about (see <see cref="InquiryDefaults.Types"/>).</summary>
    public string Type { get; set; } = InquiryDefaults.Types[0];

    /// <summary>The staff member owning the follow-up (ApplicationUser id), if assigned.</summary>
    public string? AssignedToId { get; set; }
    public ApplicationUser? Assignee { get; set; }

    /// <summary>The Parent/Student account that submitted this inquiry (null when logged by staff).</summary>
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    public InquiryStatus Status { get; set; } = InquiryStatus.New;
    public DateTime DateCreated { get; set; } = DateTime.Today;

    /// <summary>When a staff member first replied (first Parent-visible note). Difference from
    /// <see cref="DateCreated"/> yields the lead's response time for reporting.</summary>
    public DateTime? FirstResponseAt { get; set; }

    /// <summary>The Student and Enrollment records created when this lead was converted (Module 1/2 integration).</summary>
    public int? ConvertedStudentId { get; set; }
    public Student? ConvertedStudent { get; set; }
    public int? ConvertedEnrollmentId { get; set; }
    public Enrollment? ConvertedEnrollment { get; set; }

    public string StatusClass => Status switch
    {
        InquiryStatus.Approved => "text-bg-success",
        InquiryStatus.Rejected => "text-bg-danger",
        _ => "text-bg-warning"
    };

    public string? SourceClass => Source.ToLowerInvariant() switch
    {
        "website" => "text-bg-info",
        "referral" => "text-bg-success",
        _ => "text-bg-light text-body"
    };

    public ICollection<InquiryNote> Thread { get; set; } = new List<InquiryNote>();

    /// <summary>Documents the parent uploaded with their application (before Convert).</summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

/// <summary>Follow-up messages and status-change log entries for an inquiry.</summary>
public class InquiryNote
{
    public int Id { get; set; }

    public int InquiryId { get; set; }
    public Inquiry Inquiry { get; set; } = null!;

    public string? StaffId { get; set; }
    public ApplicationUser? Staff { get; set; }

    /// <summary>Who this note is visible to — "Registrar" for internal staff notes, "Parent" for parent-facing replies.</summary>
    public string Visibility { get; set; } = "Parent";

    [Required]
    public string Body { get; set; } = "";

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}