using EduCore.Models.Entities;

namespace EduCore.ViewModels;

public class ParentDashboardVM
{
    public List<ChildSummaryVM> Children { get; set; } = new();
    public decimal TotalOutstandingBalance { get; set; }
    public int UnreadMessages { get; set; }
    public int PendingInquiries { get; set; }
    public int ActiveEnrollments { get; set; }
    public List<Message> RecentMessages { get; set; } = new();
    public List<Inquiry> RecentInquiries { get; set; } = new();
    public List<Announcement> RecentAnnouncements { get; set; } = new();
}

public class ChildSummaryVM
{
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public string SectionName { get; set; } = "";
    public decimal OutstandingBalance { get; set; }
    public int UnreadMessages { get; set; }
    public EnrollmentStatus? EnrollmentStatus { get; set; }
}