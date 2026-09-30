using EduCore.Models.Entities;

namespace EduCore.ViewModels;

public class FacultyStudentListVM
{
    public PagedResult<FacultyStudentVM> Students { get; set; } = new();
    public List<Section> Sections { get; set; } = new();
    public FacultyStudentQuery Query { get; set; } = new();
}