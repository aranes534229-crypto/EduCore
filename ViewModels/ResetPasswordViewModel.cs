using System.ComponentModel.DataAnnotations;

namespace EduCore.ViewModels;

/// <summary>Admin resets a user's password to this value (clears any lockout too).</summary>
public class ResetPasswordViewModel
{
    public string UserId { get; set; } = "";

    [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
}