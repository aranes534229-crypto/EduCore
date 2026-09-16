using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

/// <summary>Module 3 — a chargeable item on a tuition invoice (Tuition, Misc, Book Deposit...).</summary>
public class Fee
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    [Range(typeof(decimal), "0", "100000000")]
    public decimal Amount { get; set; }

    public bool IsActive { get; set; } = true;
}