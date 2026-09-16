using Microsoft.AspNetCore.Identity;

namespace EduCore.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}