using EduCore.Models.Entities;

namespace EduCore.ViewModels;

public class FacultyStudentVM
{
    public int Id { get; set; }
    public string StudentNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string SectionName { get; set; } = "";
    public string GradeLevelName { get; set; } = "";
    public string GuardianName { get; set; } = "";
    public string GuardianContact { get; set; } = "";
    public string GuardianEmail { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime? BirthDate { get; set; }
    public List<GradeSummaryVM> Grades { get; set; } = new();
    public AttendanceSummaryVM Attendance { get; set; } = new();
    public List<Document> Documents { get; set; } = new();
}

public class GradeSummaryVM
{
    public int SectionSubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public int? Score { get; set; }
    public string Remarks { get; set; } = "";
}

public class AttendanceSummaryVM
{
    public int TotalDays { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public double AttendanceRate => TotalDays > 0 ? (double)PresentCount / TotalDays * 100 : 0;
}