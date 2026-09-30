using EduCore.Models.Entities;

namespace EduCore.ViewModels;

public class FacultyIndexVM
{
    public IReadOnlyList<Faculty> Items { get; set; } = Array.Empty<Faculty>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public string? Search { get; set; }
    public bool? IsActive { get; set; }

    public int[] PageSizeValues { get; } = new[] { 5, 10, 25, 50 };
}
