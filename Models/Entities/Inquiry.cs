namespace EduCore.Models.Entities;

/// <summary>A prospective-student lead logged by the Registrar before an application exists.
/// "Converted" is the signal that a Student + Enrollment should be created (see those screens).</summary>
public enum InquiryStatus { New, InProgress, Closed, Converted }

public class Inquiry
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string StudentName { get; set; } = "";

    public int GradeLevelId { get; set; }
    public GradeLevel GradeLevel { get; set; } = null!;

    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string? Notes { get; set; }

    /// <summary>The staff member owning the follow-up (ApplicationUser id), if assigned.</summary>
    public string? AssignedToId { get; set; }

    public InquiryStatus Status { get; set; } = InquiryStatus.New;
    public DateTime DateCreated { get; set; } = DateTime.Today;

    public string StatusClass => Status switch
    {
        InquiryStatus.Converted => "text-bg-success",
        InquiryStatus.Closed => "text-bg-secondary",
        InquiryStatus.InProgress => "text-bg-primary",
        _ => "text-bg-warning"
    };
}