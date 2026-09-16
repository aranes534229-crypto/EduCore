using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>A teacher's record. Their login account links in via ApplicationUserId.</summary>
public class Faculty
{
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; } = "";

    [Required]
    public string LastName { get; set; } = "";

    [EmailAddress]
    public string Email { get; set; } = "";

    public string Contact { get; set; } = "";

    /// <summary>Login of the Faculty account, if one was created.</summary>
    public string? ApplicationUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";
}