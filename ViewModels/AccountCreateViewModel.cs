using System.ComponentModel.DataAnnotations;

namespace EduCore.ViewModels;

/// <summary>Admin provisioning form: create a login account and assign one role.
/// Unlike RegisterViewModel, this is Admin-only and picks a role.</summary>
public class AccountCreateViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, Display(Name = "Full name")]
    public string DisplayName { get; set; } = "";

    [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [Required]
    public string Role { get; set; } = "";
}