namespace EduCore.Models.Entities;

/// <summary>An academic year (e.g. 2025–2026). Enrollment, billing and scheduling hang off this.</summary>
public class SchoolYear
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}