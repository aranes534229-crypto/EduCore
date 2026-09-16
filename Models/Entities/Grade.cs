using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>A score a student earned in one subject-in-a-section.</summary>
public class Grade
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SectionSubjectId { get; set; }
    public SectionSubject SectionSubject { get; set; } = null!;

    [Range(0, 100)]
    public int Score { get; set; }

    public string Remarks { get; set; } = "";
}