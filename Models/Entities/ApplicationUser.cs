using Microsoft.AspNetCore.Identity;

namespace EduCore.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
}