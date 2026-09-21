namespace EduCore.ViewModels;

/// <summary>One row in the Admin Accounts list.</summary>
public class UserListItemVM
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public IList<string> Roles { get; set; } = new List<string>();
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsLocked => LockoutEnd is not null && LockoutEnd > DateTimeOffset.UtcNow;
}