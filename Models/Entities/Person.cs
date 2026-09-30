using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

public class Person
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = "";

    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = "";

    [MaxLength(20)]
    public string Phone { get; set; } = "";

    [MaxLength(500)]
    public string Address { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public ApplicationUser? User { get; set; }
    public Faculty? Faculty { get; set; }
    public Student? Student { get; set; }
}