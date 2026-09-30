using EduCore.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduCore.ViewModels;

public class ExpensesIndexVM
{
    public IReadOnlyList<Expense> Items { get; set; } = Array.Empty<Expense>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public string? Search { get; set; }
    public string? Category { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public SelectList Categories { get; set; } = default!;

    // Ledger-level summary tiles — always computed from ALL expenses, never the filtered set.
    public decimal Collected { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Net => Collected - TotalExpenses;

    public int[] PageSizeValues { get; } = new[] { 5, 10, 25, 50 };
}
