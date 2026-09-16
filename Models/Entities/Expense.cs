using System.ComponentModel.DataAnnotations;

namespace EduCore.Models.Entities;

public class Expense
{
    public int Id { get; set; }

    [Required] public string Description { get; set; } = "";

    // Free-text for grouping/saving us a table; a Category entity is premature.
    public string? Category { get; set; }

    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal Amount { get; set; }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;
}