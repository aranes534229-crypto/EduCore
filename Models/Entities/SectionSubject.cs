using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>A subject as taught in a specific section, and which teacher teaches it.
/// One faculty teaches (subject, section) — the "handles each subject per section".</summary>
public class SectionSubject
{
    public int Id { get; set; }

    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int? FacultyId { get; set; }
    public Faculty? Faculty { get; set; }

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}