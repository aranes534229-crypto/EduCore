namespace EduCore.ViewModels;

public class RegistrarDashboardVM
{
    public int NewInquiries { get; set; }
    public int ApprovedInquiries { get; set; }
    public int RejectedInquiries { get; set; }

    public int UnscheduledEnrollments { get; set; }
    public int ScheduledEnrollments { get; set; }
    public int TotalEnrollments { get; set; }

    public int ActiveSections { get; set; }
    public int EnrolledStudents { get; set; }
}