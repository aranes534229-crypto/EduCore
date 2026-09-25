using System.ComponentModel.DataAnnotations;
using EduCore.Models.Entities;

namespace EduCore.ViewModels;

/// <summary>Form model for creating/editing a Fee with multiple line items.
/// Reuses the row-VM pattern from EntryRowVM (see ViewModels/EntryRowVM.cs).</summary>
public class FeeFormVm
{
    public Fee Fee { get; set; } = new();

    public List<FeeLineItemVm> Items { get; set; } = new();
}

public class FeeLineItemVm
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "A description is required.")]
    public string Description { get; set; } = "";

    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Enter a valid amount.")]
    public decimal Amount { get; set; }

    public bool IsActive { get; set; } = true;
}
