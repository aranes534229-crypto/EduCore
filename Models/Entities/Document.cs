namespace EduCore.Models.Entities;

/// <summary>An uploaded file (birth cert, report card, photo) attached to a student record, or to a
/// pending application (an Inquiry) before a Student exists. On Convert the application's documents
/// are reassigned to the created Student (see InquiriesController.Convert).</summary>
public class Document
{
    public int Id { get; set; }
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
    /// <summary>Set for documents a parent uploads while applying, before Convert creates the Student.</summary>
    public int? InquiryId { get; set; }
    public Inquiry? Inquiry { get; set; }
    public string FileName { get; set; } = "";
    /// <summary>Relative path under wwwroot/uploads.</summary>
    public string Path { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.Now;
}