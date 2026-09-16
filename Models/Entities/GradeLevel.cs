namespace EduCore.Models.Entities;

/// <summary>A grade the school teaches (Kindergarden/Grade 1 … Grade 12).</summary>
public class GradeLevel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}