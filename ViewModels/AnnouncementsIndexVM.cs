using EduCore.Models.Entities;

namespace EduCore.ViewModels;

public class AnnouncementsIndexVM
{
    public IReadOnlyList<Announcement> Items { get; set; } = Array.Empty<Announcement>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public string? Search { get; set; }
    public bool CanPost { get; set; }

    public int[] PageSizeValues { get; } = new[] { 5, 10, 25, 50 };
}
