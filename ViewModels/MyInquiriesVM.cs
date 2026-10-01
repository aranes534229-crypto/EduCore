using EduCore.Models.Entities;

namespace EduCore.ViewModels;

/// <summary>Parent/Student: paged list of the logged-in user's own inquiries.</summary>
public class MyInquiriesVM
{
    public IReadOnlyList<Inquiry> Items { get; set; } = Array.Empty<Inquiry>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
