using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();
    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Faculty> Faculty => Set<Faculty>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<SectionSubject> SectionSubjects => Set<SectionSubject>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Attendance> Attendance => Set<Attendance>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Fee> Fees => Set<Fee>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Roles are seeded at runtime (see DbSeeder via RoleManager) rather than via
        // HasData, because IdentityRole uses dynamic values that EF flags as a
        // non-deterministic model (PendingModelChangesWarning).
        b.Entity<Student>()
            .HasOne(s => s.Section)
            .WithMany(sec => sec.Students)
            .HasForeignKey(s => s.SectionId)
            .OnDelete(DeleteBehavior.SetNull);

        // A subject meets at most once per section; the teacher for that meeting changes.
        b.Entity<SectionSubject>()
            .HasIndex(ss => new { ss.SectionId, ss.SubjectId })
            .IsUnique();

        // A student applies once per school year.
        b.Entity<Enrollment>()
            .HasIndex(e => new { e.StudentId, e.SchoolYearId })
            .IsUnique();

        // Money columns are decimal(18,2) so amounts don't silently truncate.
        b.Entity<Fee>().Property(f => f.Amount).HasPrecision(18, 2);
        b.Entity<InvoiceLine>().Property(l => l.Amount).HasPrecision(18, 2);
        b.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);
        b.Entity<Expense>().Property(e => e.Amount).HasPrecision(18, 2);

        b.Entity<GradeLevel>().HasData(
            Enumerable.Range(1, 12)
                .Select(i => new GradeLevel { Id = i, Name = $"Grade {i}", SortOrder = i }));
    }
}