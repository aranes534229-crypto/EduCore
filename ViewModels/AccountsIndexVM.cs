using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduCore.ViewModels;

public class AccountsIndexVM
{
    public IReadOnlyList<UserListItemVM> Items { get; set; } = Array.Empty<UserListItemVM>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool? Locked { get; set; }

    public string CurrentUserId { get; set; } = "";
    public SelectList RoleValues { get; set; } = default!;
    public int[] PageSizeValues { get; } = new[] { 5, 10, 25, 50 };
}
