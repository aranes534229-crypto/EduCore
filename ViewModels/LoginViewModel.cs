using System.ComponentModel.DataAnnotations;

namespace EduCore.ViewModels;

/// <summary>Minimal login form model for the styled AccountController login page.</summary>
public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
