namespace EduCore.ViewModels;

public class FacultyStudentQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    public int? SectionId { get; set; }
    public string? SortColumn { get; set; }
    public bool SortDesc { get; set; }
}