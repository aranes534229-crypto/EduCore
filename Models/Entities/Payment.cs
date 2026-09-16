using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>Module 3 — a payment toward an invoice. Method is free text (Cash / GCash / Bank...).</summary>
public class Payment
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal Amount { get; set; }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    public string Method { get; set; } = "";
    public string? Reference { get; set; }
}