using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>A student's official record. Guardian contact lives here for the Parent portal.
/// A parent's login links in via ApplicationUserId.</summary>
public class Student
{
    public int Id { get; set; }

    /// <summary>Admission number (e.g. "1001") assigned when a student is created. Sequential
    /// starting at 1001, count-based so deleted students don't leave gaps. Kept as a display
    /// string — the DB int Id stays the key.</summary>
    public string StudentNumber { get; set; } = "";

    public DateTime? BirthDate { get; set; }
    public string GuardianName { get; set; } = "";
    public string GuardianContact { get; set; } = "";
    public string GuardianEmail { get; set; } = "";

    public int? PersonId { get; set; }
    public Person? Person { get; set; }

    /// <summary>The section the student is grouped into for the school year (set by Registrar).</summary>
    public int? SectionId { get; set; }
    public Section? Section { get; set; }

    /// <summary>Stored name — filled when the student is created without a Person link (e.g. an
    /// inquiry converted for a sibling whose parent contact already owns a Student record).
    /// For linked students the Person's name stays authoritative.</summary>
    [MaxLength(100)]
    public string FirstName { get; set; } = "";
    [MaxLength(100)]
    public string LastName { get; set; } = "";

    public string FullName => Person?.FullName ?? $"{FirstName} {LastName}".Trim();
    public string Email => Person?.Email ?? "";
    public string Address => Person?.Address ?? "";

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Attendance> Attendance { get; set; } = new List<Attendance>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}