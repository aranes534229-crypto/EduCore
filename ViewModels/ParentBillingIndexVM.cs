using EduCore.Models.Entities;

namespace EduCore.ViewModels;

/// <summary>Tuition & Billing, parent self-serve: paged list of the logged-in parent's statements.</summary>
public class ParentBillingIndexVM
{
    public IReadOnlyList<Invoice> Items { get; set; } = Array.Empty<Invoice>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
