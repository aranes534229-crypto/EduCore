using EduCore.Models.Entities;

namespace EduCore.ViewModels;

public class MoneyReportVM
{
    public IReadOnlyList<Invoice> Items { get; set; } = Array.Empty<Invoice>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public string? Search { get; set; }

    // Global figures — computed from ALL invoices, never the filtered set.
    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Net => TotalOutstanding - TotalExpenses;

    public int[] PageSizeValues { get; } = new[] { 5, 10, 25, 50 };
}
