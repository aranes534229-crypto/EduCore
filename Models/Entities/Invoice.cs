using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>Module 3 — a bill for one student for a school year. Lines snapshot the fees at issue
/// time; balance is Total minus all Payments, no separate amount column to keep in sync.</summary>
public class Invoice
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SchoolYearId { get; set; }
    public SchoolYear SchoolYear { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime IssuedDate { get; set; } = DateTime.Today;

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public decimal Total => Lines.Sum(l => l.Amount);
    public decimal Paid => Payments.Sum(p => p.Amount);
    public decimal Balance => Total - Paid;
}