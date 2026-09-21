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
    public DbSet<InquiryNote> InquiryNotes => Set<InquiryNote>();

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
        b.Entity<GradeLevel>().Property(g => g.Amount).HasPrecision(18, 2);

        // Demo tuition: a little more per grade step so the billing flow shows realistic numbers.
        b.Entity<GradeLevel>().HasData(
            Enumerable.Range(1, 12)
                .Select(i => new GradeLevel { Id = i, Name = $"Grade {i}", SortOrder = i, Amount = 10000m + 1500m * i }));

        b.Entity<InquiryNote>()
            .HasOne(n => n.Inquiry)
            .WithMany(i => i.Thread)
            .HasForeignKey(n => n.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<InquiryNote>()
            .HasOne(n => n.Staff)
            .WithMany()
            .HasForeignKey(n => n.StaffId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<Inquiry>()
            .HasOne(i => i.Assignee)
            .WithMany()
            .HasForeignKey(i => i.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<Inquiry>()
            .HasOne(i => i.CreatedBy)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict, not Cascade: SQL Server forbids a Cascade path into Inquiries from
        // GradeLevel (creates multiple-cascade-path conflict in the delete graph), and
        // it's wrong anyway to delete a grade level that still has inquiries.
        b.Entity<Inquiry>()
            .HasOne(i => i.GradeLevel)
            .WithMany()
            .HasForeignKey(i => i.GradeLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict for both conversions: SQL Server 1785 forbids any second cascade/set-null
        // path into Inquiries from a single Student delete (it would reach the row via
        // ConvertedStudentId and again via Enrollments -> ConvertedEnrollmentId). Blocking
        // the delete of a converted student/enrollment is preferred to the ambiguity.
        b.Entity<Inquiry>()
            .HasOne(i => i.ConvertedStudent)
            .WithMany()
            .HasForeignKey(i => i.ConvertedStudentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Inquiry>()
            .HasOne(i => i.ConvertedEnrollment)
            .WithMany()
            .HasForeignKey(i => i.ConvertedEnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}