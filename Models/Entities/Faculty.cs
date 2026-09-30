using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>A teacher's record. Their profile links in via Person.</summary>
public class Faculty
{
    public int Id { get; set; }

    [Required]
    public string EmployeeNumber { get; set; } = "";

    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    public int? PersonId { get; set; }
    public Person? Person { get; set; }

    public bool IsActive { get; set; } = true;

    public string FullName => Person?.FullName ?? "";
    public string Email => Person?.Email ?? "";
    public string Contact => Person?.Phone ?? "";
}