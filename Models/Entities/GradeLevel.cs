namespace EduCore.Models.Entities;

/// <summary>A grade the school teaches (Kindergarden/Grade 1 … Grade 12).</summary>
public class GradeLevel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }

    /// <summary>Whole-year tuition for this grade. When a student is scheduled, an invoice is
    /// auto-created from this amount. For optional/additional per-grade fees (books, labs,
    /// discounts, installments), add a GradeFee table instead of more columns here.</summary>
    public decimal Amount { get; set; }
}