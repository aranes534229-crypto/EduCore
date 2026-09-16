using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>Module 3 — one line on an invoice. Description/Amount are snapshots so later edits to a
/// Fee don't rewrite already-issued bills.</summary>
public class InvoiceLine
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    [Required]
    public string Description { get; set; } = "";

    [Range(typeof(decimal), "0", "100000000")]
    public decimal Amount { get; set; }
}