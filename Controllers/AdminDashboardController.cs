using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduCore.Data;
using EduCore.ViewModels;

namespace EduCore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _db;
        public AdminDashboardController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var openInvoices = await _db.Invoices
                .Where(i => i.Lines.Sum(l => l.Amount) > i.Payments.Sum(p => p.Amount))
                .CountAsync();

            var vm = new AdminDashboardVM
            {
                TotalStudents = await _db.Students.CountAsync(),
                TotalFaculty = await _db.Faculty.CountAsync(),
                TotalInquiries = await _db.Inquiries.CountAsync(),
                OpenInvoices = openInvoices
            };
            return View(vm);
        }
    }
}