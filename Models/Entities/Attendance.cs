using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>Daily attendance for a student in their section.</summary>
public class Attendance
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    // Present | Absent | Late | Excused
    [Required]
    public string Status { get; set; } = "Present";
}