using EduCore.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduCore.ViewModels;

public class InquiriesIndexVM
{
    public IReadOnlyList<Inquiry> Items { get; set; } = Array.Empty<Inquiry>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public string? Search { get; set; }
    public InquiryStatus? Status { get; set; }
    public int? GradeLevelId { get; set; }

    public SelectList GradeLevels { get; set; } = default!;
    public int[] PageSizeValues { get; } = new[] { 5, 10, 25, 50 };
    public SelectList StatusOptions { get; } = new SelectList(Enum.GetValues(typeof(InquiryStatus)).Cast<InquiryStatus>().Select(s => new { Value = (int?)s, Text = s.ToString() }), "Value", "Text");
}