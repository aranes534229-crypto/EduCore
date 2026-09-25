using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>One charge line on a Fee, e.g. "Tuition", "Registration Fee", "Lab Fee".
/// A Fee can have multiple lines; their sum is the fee total applied to an invoice.</summary>
public class FeeLine
{
    public int Id { get; set; }

    public int FeeId { get; set; }
    public Fee Fee { get; set; } = null!;

    [Required]
    public string Description { get; set; } = "";

    [Range(typeof(decimal), "0", "100000000")]
    public decimal Amount { get; set; }

    /// <summary>Whether this line is currently active. Inactive lines are excluded
    /// from the fee total and from new invoices, but are not deleted so historical
    /// snapshots stay intact.</summary>
    public bool IsActive { get; set; } = true;
}
