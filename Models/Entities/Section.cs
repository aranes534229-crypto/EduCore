using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>A class group: students of one grade level in one school year, with an adviser.</summary>
public class Section
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    public int GradeLevelId { get; set; }
    public GradeLevel GradeLevel { get; set; } = null!;

    public int SchoolYearId { get; set; }
    public SchoolYear SchoolYear { get; set; } = null!;

    /// <summary>Optional homeroom teacher.</summary>
    public int? AdviserId { get; set; }
    public Faculty? Adviser { get; set; }

    public ICollection<SectionSubject> Subjects { get; set; } = new List<SectionSubject>();
    public ICollection<Student> Students { get; set; } = new List<Student>();
}