namespace EduCore.Models.Entities;

/// <summary>An uploaded file (birth cert, report card, etc.) attached to a student record.</summary>
public class Document
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string FileName { get; set; } = "";
    /// <summary>Relative path under wwwroot/uploads.</summary>
    public string Path { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.Now;

    public Student? Student { get; set; }
}