using EduCore.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduCore.ViewModels;

public class EnrollmentReportVM
{
    public IReadOnlyList<Enrollment> Items { get; set; } = Array.Empty<Enrollment>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public string? Search { get; set; }

    // Tiles reflect the selected school year, regardless of search/paging.
    public Dictionary<EnrollmentStatus, int> Totals { get; set; } = new();
    public int Total { get; set; }

    public SelectList SchoolYears { get; set; } = default!;
    public int? SelectedYear { get; set; }
    public string YearName { get; set; } = "(none)";

    public int[] PageSizeValues { get; } = new[] { 5, 10, 25, 50 };
}
