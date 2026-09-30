using EduCore.Models.Entities;

namespace EduCore.ViewModels
{
    public class FinanceDashboardVM
    {
        // Revenue KPIs (active school year)
        public decimal TotalBilled { get; set; }       // sum of invoice totals
        public decimal TotalCollected { get; set; }    // sum of payments
        public decimal OutstandingReceivables { get; set; } // Billed - Collected
        public int OpenInvoicesCount { get; set; }
        public int OverdueInvoicesCount { get; set; }

        // Expense KPIs (active school year)
        public decimal TotalExpenses { get; set; }
        public decimal NetPosition { get; set; }       // Collected - Expenses

        // Breakdown for cards
        public decimal TuitionCollected { get; set; }
        public decimal OtherFeesCollected { get; set; }

        // Recent activity (last 10 each)
        public List<Payment> RecentPayments { get; set; } = new();
        public List<Invoice> OverdueInvoices { get; set; } = new();
        public List<Expense> RecentExpenses { get; set; } = new();

        // School year context
        public string ActiveSchoolYearName { get; set; } = "\u2014";
    }
}