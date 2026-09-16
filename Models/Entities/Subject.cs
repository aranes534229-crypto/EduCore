using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>A subject taught at the school (Math, English, Science...).</summary>
public class Subject
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = "";

    [Required]
    public string Name { get; set; } = "";

    public string FullName => $"{Code} — {Name}";

    public ICollection<SectionSubject> SectionSubjects { get; set; } = new List<SectionSubject>();
}