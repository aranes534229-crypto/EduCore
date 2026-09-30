using EduCore.Models.Entities;

namespace EduCore.ViewModels;

public class FacultyDashboardVM
{
    public int AdvisedSectionsCount { get; set; }
    public int TeachingSubjectsCount { get; set; }
    public int TotalStudents { get; set; }
    public int UnreadMessages { get; set; }
    public List<AttendanceDateVM> UpcomingAttendance { get; set; } = new();
    public List<Announcement> RecentAnnouncements { get; set; } = new();
}

public class AttendanceDateVM
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = "";
    public DateTime Date { get; set; }
}