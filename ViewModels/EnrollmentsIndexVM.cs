using EduCore.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduCore.ViewModels;

public class EnrollmentsIndexVM
{
    public IReadOnlyList<Enrollment> Items { get; set; } = Array.Empty<Enrollment>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public string? Search { get; set; }
    public EnrollmentStatus? Status { get; set; }
    public int? GradeLevelId { get; set; }
    public int? SchoolYearId { get; set; }

    public SelectList GradeLevels { get; set; } = default!;
    public SelectList SchoolYears { get; set; } = default!;
    public int[] PageSizeValues { get; } = new[] { 5, 10, 25, 50 };
}
