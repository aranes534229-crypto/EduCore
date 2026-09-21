using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduCore.Models.Entities;

/// <summary>A class group: students of one grade level in one school year, with an adviser.</summary>
public class Section
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    public int GradeLevelId { get; set; }
    [ValidateNever]  // EF-fed; not bound from a form, so MVC must not require it
    public GradeLevel GradeLevel { get; set; } = null!;

    public int SchoolYearId { get; set; }
    [ValidateNever]
    public SchoolYear SchoolYear { get; set; } = null!;

    /// <summary>Optional homeroom teacher.</summary>
    public int? AdviserId { get; set; }
    public Faculty? Adviser { get; set; }

    /// <summary>Set by the Registrar when the roster is handed to Finance for billing.</summary>
    public DateTime? SubmittedToFinanceAt { get; set; }
    public bool IsSubmitted => SubmittedToFinanceAt.HasValue;

    public ICollection<SectionSubject> Subjects { get; set; } = new List<SectionSubject>();
    public ICollection<Student> Students { get; set; } = new List<Student>();
}