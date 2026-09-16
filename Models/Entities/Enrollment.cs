using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

public enum EnrollmentStatus { Applied, Approved, Rejected, Withdrawn }

/// <summary>Module 2 — an application for a student to enter a grade in a school year.
/// Approval is lifecycle only; placing the accepted student in a section stays in Module 5.</summary>
public class Enrollment
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SchoolYearId { get; set; }
    public SchoolYear SchoolYear { get; set; } = null!;

    public int GradeLevelId { get; set; }
    public GradeLevel GradeLevel { get; set; } = null!;

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Applied;

    [DataType(DataType.Date)]
    public DateTime ApplicationDate { get; set; } = DateTime.Today;

    public string? Notes { get; set; }

    /// <summary>Bootstrap badge class for the status, for the Index view.</summary>
    public string StatusClass => Status switch
    {
        EnrollmentStatus.Approved => "text-bg-success",
        EnrollmentStatus.Rejected => "text-bg-danger",
        EnrollmentStatus.Withdrawn => "text-bg-warning",
        _ => "text-bg-secondary"
    };
}