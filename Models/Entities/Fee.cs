using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduCore.Models.Entities;

/// <summary>Module 3 — a chargeable fee. A fee has one or more line items
/// (tuition, registration, lab, etc.) whose amounts sum to the total. Fee lines
/// are snapshot on invoice issue (see InvoiceLine.Description/Amount), so editing
/// a fee later never rewrites already-issued bills.</summary>
public class Fee
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;

    /// <summary>If set, this is the master tuition fee for that grade — auto-applied
    /// when a student of that grade is scheduled. Null = a shared/extra fee.</summary>
    public int? GradeLevelId { get; set; }

    [ValidateNever]
    public GradeLevel? GradeLevel { get; set; }

    public ICollection<FeeLine> Lines { get; set; } = new List<FeeLine>();

    /// <summary>Total charge = sum of active line items. Computed, not stored, so it
    /// can never drift from the lines.</summary>
    public decimal TotalAmount => Lines?.Where(l => l.IsActive).Sum(l => l.Amount) ?? 0m;
}
