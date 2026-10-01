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
            int openInvoices = 0;
            try
            {
                var invoices = await _db.Invoices
                    .Include(i => i.Lines)
                    .Include(i => i.Payments)
                    .ToListAsync();

                openInvoices = invoices.Count(i => i.Balance > 0);
            }
            catch (Exception)
            {
                openInvoices = 0;
            }

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