namespace EduCore.ViewModels;

/// <summary>One editable row in the grade-entry or attendance sheet. Scores/status
/// for every student in a section are posted back as a list of these.</summary>
public class GradeRowVM
{
    public int StudentId { get; set; }
    // Nullable: null means "not graded yet", distinct from a real 0.
    // An empty number input posts back as null, and the POST skips
    // persistence for those rows (see GradebookController.Grades).
    [System.ComponentModel.DataAnnotations.Range(0, 100)]
    public int? Score { get; set; }
    public string Remarks { get; set; } = "";
}

public class AttendanceRowVM
{
    public int StudentId { get; set; }
    public string Status { get; set; } = "Present";
}