using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduCore.Models.Entities;

public enum EnrollmentStatus { Unscheduled, Scheduled }

/// <summary>Module 2 — an application for a student to enter a grade in a school year.
/// Status is flow-driven: Unscheduled until the Registrar places the student in a section
/// (SectionController.AssignStudent flips it to Scheduled).</summary>
public class Enrollment
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    [ValidateNever]  // EF-fed; not bound from a form, so MVC must not require it
    public Student Student { get; set; } = null!;

    public int SchoolYearId { get; set; }
    [ValidateNever]
    public SchoolYear SchoolYear { get; set; } = null!;

    public int GradeLevelId { get; set; }
    [ValidateNever]
    public GradeLevel GradeLevel { get; set; } = null!;

    // Placement-based: Scheduled once the Registrar places the student in a section; Unscheduled before.
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Unscheduled;

    [DataType(DataType.Date)]
    public DateTime ApplicationDate { get; set; } = DateTime.Today;

    public string? Notes { get; set; }

    /// <summary>Bootstrap badge class for the status, for the Index view.</summary>
    public string StatusClass => Status switch
    {
        EnrollmentStatus.Scheduled => "text-bg-info",
        _ => "text-bg-secondary"
    };
}