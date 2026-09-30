using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduCore.Data;
using EduCore.Models.Entities;
using EduCore.ViewModels;

namespace EduCore.Controllers
{
    [Authorize(Roles = "Registrar")]
    public class RegistrarDashboardController : Controller
    {
        private readonly AppDbContext _db;
        public RegistrarDashboardController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            // Active school year
            var activeYear = await _db.SchoolYears.FirstOrDefaultAsync(y => y.IsActive);

            // Inquiry pipeline (all time)
            var newInq = await _db.Inquiries.CountAsync(i => i.Status == InquiryStatus.New);
            var approvedInq = await _db.Inquiries.CountAsync(i => i.Status == InquiryStatus.Approved);
            var rejectedInq = await _db.Inquiries.CountAsync(i => i.Status == InquiryStatus.Rejected);

            // Enrollment pipeline (active year if exists)
            var enrollments = _db.Enrollments.AsQueryable();
            if (activeYear != null)
                enrollments = enrollments.Where(e => e.SchoolYearId == activeYear.Id);

            var unscheduled = await enrollments.CountAsync(e => e.Status == EnrollmentStatus.Unscheduled);
            var scheduled = await enrollments.CountAsync(e => e.Status == EnrollmentStatus.Scheduled);
            var totalEnroll = await enrollments.CountAsync();

            // Sections (active year)
            var sections = _db.Sections.AsQueryable();
            if (activeYear != null)
                sections = sections.Where(s => s.SchoolYearId == activeYear.Id);

            var activeSections = await sections.CountAsync();
            var enrolledStudents = await sections.SelectMany(s => s.Students).CountAsync();

            var vm = new RegistrarDashboardVM
            {
                NewInquiries = newInq,
                ApprovedInquiries = approvedInq,
                RejectedInquiries = rejectedInq,
                UnscheduledEnrollments = unscheduled,
                ScheduledEnrollments = scheduled,
                TotalEnrollments = totalEnroll,
                ActiveSections = activeSections,
                EnrolledStudents = enrolledStudents
            };
            return View(vm);
        }
    }
}