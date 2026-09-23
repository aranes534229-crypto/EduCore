using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduCore.Models.Entities;

/// <summary>Module 3 — a chargeable item on a tuition invoice. Master tuition fees are
/// one per GradeLevel (Name = "Grade N Tuition"); extra fees (lab, activity, etc.)
/// are shared rows with GradeLevelId null.</summary>
public class Fee
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    [Range(typeof(decimal), "0", "100000000")]
    public decimal Amount { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>If set, this is the master tuition fee for that grade — auto-applied
    /// when a student of that grade is scheduled. Null = a shared/extra fee.</summary>
    public int? GradeLevelId { get; set; }

    [ValidateNever]
    public GradeLevel? GradeLevel { get; set; }
}