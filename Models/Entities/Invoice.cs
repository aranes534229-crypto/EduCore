using System.ComponentModel.DataAnnotations;
using EduCore.Data;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Models.Entities;

/// <summary>Module 3 — a bill for one student for a school year. Lines snapshot the fees at issue
/// time; balance is Total minus all Payments, no separate amount column to keep in sync.</summary>
public class Invoice
{
    public int Id { get; set; }

    [MaxLength(16)]
    public string? Number { get; set; }

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

/// <summary>Sequential invoice-number generator, e.g. "INV-0007". Derived from the max row id so
/// it always advances and never reuses a number, even after deletions.</summary>
public static class InvoiceNumbering
{
    public static async Task<string> NextAsync(AppDbContext db)
    {
        var last = await db.Invoices.MaxAsync(i => (int?)i.Id) ?? 0;
        return $"INV-{last + 1:D4}";
    }
}