using System.ComponentModel.DataAnnotations;

namespace EduCore.ViewModels;

/// <summary>Self-registration form model. Hard-wires the Parent role — staff roles are seeded/admin-only
/// (see proposal) so exposing a role picker here would be a security footgun.</summary>
public class RegisterViewModel
{
    [Required, Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
