using EduCore.Models.Constants;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Data;

/// <summary>Dev-time seeding only: a default admin + one record of each core value.
/// Roles are seeded at runtime via RoleManager; GradeLevels are seeded by the migration's HasData.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
    {
        foreach (var roleName in AppRoles.All)
            if (!await roles.RoleExistsAsync(roleName))
                await roles.CreateAsync(new IdentityRole(roleName));

        if ((await users.FindByEmailAsync("admin@educore.local")) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@educore.local",
                Email = "admin@educore.local",
                DisplayName = "System Admin",
                EmailConfirmed = true
            };
            var result = await users.CreateAsync(admin, "Admin123!");
            if (result.Succeeded) await users.AddToRoleAsync(admin, AppRoles.Admin);
        }

        // Finance has a module now (Module 6), so it gets a demo login like Faculty does.
        if ((await users.FindByEmailAsync("finance@educore.local")) is null)
        {
            var finance = new ApplicationUser
            {
                UserName = "finance@educore.local",
                Email = "finance@educore.local",
                DisplayName = "Lina Reyes",
                EmailConfirmed = true
            };
            var result = await users.CreateAsync(finance, "Finance123!");
            if (result.Succeeded) await users.AddToRoleAsync(finance, AppRoles.Finance);
        }

        // Module 7 — a Parent login (Juan's guardian) so teacher↔parent messaging is testable.
        if ((await users.FindByEmailAsync("parent@educore.local")) is null)
        {
            var parent = new ApplicationUser
            {
                UserName = "parent@educore.local",
                Email = "parent@educore.local",
                DisplayName = "Maria Dela Cruz",
                EmailConfirmed = true
            };
            var result = await users.CreateAsync(parent, "Parent123!");
            if (result.Succeeded) await users.AddToRoleAsync(parent, AppRoles.Parent);
        }

        // Module 8 — Registrar's module is Inquiry & Admission, so Registrar gets a login too.
        if ((await users.FindByEmailAsync("reg@educore.local")) is null)
        {
            var registrar = new ApplicationUser
            {
                UserName = "reg@educore.local",
                Email = "reg@educore.local",
                DisplayName = "Rosa Tan",
                EmailConfirmed = true
            };
            var result = await users.CreateAsync(registrar, "Registrar123!");
            if (result.Succeeded) await users.AddToRoleAsync(registrar, AppRoles.Registrar);
        }

        if (!await db.SchoolYears.AnyAsync())
            db.SchoolYears.Add(new SchoolYear
            {
                Name = "2025–2026",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2026, 3, 31),
                IsActive = true
            });

        if (!await db.Students.AnyAsync())
            db.Students.Add(new Student
            {
                FirstName = "Juan",
                LastName = "Dela Cruz",
                BirthDate = new DateTime(2016, 3, 14),
                Address = "123 Mabini St.",
                GuardianName = "Maria Dela Cruz",
                GuardianContact = "0917-000-0000",
                GuardianEmail = "maria.delacruz@example.com"
            });

        if (!await db.Faculty.AnyAsync())
        {
            var teacher = new Faculty
            {
                FirstName = "Ana",
                LastName = "Santos",
                Email = "teacher@educore.local",
                Contact = "0918-000-0000",
                IsActive = true
            };
            db.Faculty.Add(teacher);

            // Give the demo teacher a login in the Faculty role so the portal is testable.
            if ((await users.FindByEmailAsync(teacher.Email)) is null)
            {
                var acct = new ApplicationUser
                {
                    UserName = teacher.Email,
                    Email = teacher.Email,
                    DisplayName = teacher.FullName,
                    EmailConfirmed = true
                };
                var result = await users.CreateAsync(acct, "Teacher123!");
                if (result.Succeeded)
                {
                    await users.AddToRoleAsync(acct, AppRoles.Faculty);
                    teacher.ApplicationUserId = acct.Id;
                }
            }
        }

        await db.SaveChangesAsync();

        // Module 7 — link Juan to his Parent login so teacher↔parent messaging has a recipient.
        var parentUser = await users.FindByEmailAsync("parent@educore.local");
        var juanLink = await db.Students.FirstOrDefaultAsync(s => s.LastName == "Dela Cruz");
        if (parentUser is not null && juanLink is not null && juanLink.ApplicationUserId is null)
        {
            juanLink.ApplicationUserId = parentUser.Id;
            await db.SaveChangesAsync();
        }

        // Module 8 — one lead inquiry, assigned to the Registrar for follow-up.
        var regUser = await users.FindByEmailAsync("reg@educore.local");
        if (regUser is not null && !await db.Inquiries.AnyAsync())
        {
            db.Inquiries.Add(new Inquiry
            {
                StudentName = "Karla Reyes",
                GradeLevelId = 1,
                ContactEmail = "karla.reyes@example.com",
                ContactPhone = "0919-000-0000",
                Notes = "Walk-in visit; interested in Grade 1.",
                AssignedToId = regUser.Id,
                Status = InquiryStatus.New,
                DateCreated = DateTime.Today.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        // Module 7 — one welcome announcement so the CMS board is populated.
        if (!await db.Announcements.AnyAsync())
        {
            db.Announcements.Add(new Announcement
            {
                Title = "Welcome to School Year 2025–2026",
                Body = "Classes begin June 1. Uniforms and supplies are available at the campus bookstore.",
                AuthorName = "System Admin",
                PostedAt = DateTime.UtcNow.AddDays(-2)
            });
            await db.SaveChangesAsync();
        }

        // Module 5 — Class Scheduling demo data.
        if (!await db.Subjects.AnyAsync())
        {
            db.Subjects.AddRange(new[]
            {
                new Subject { Code = "MATH", Name = "Mathematics" },
                new Subject { Code = "ENG",  Name = "English" },
                new Subject { Code = "SCI",  Name = "Science" },
                new Subject { Code = "FIL",  Name = "Filipino" }
            });
            await db.SaveChangesAsync();
        }

        // Module 3 — Tuition & Billing demo data: a couple of default fees, no invoice
        // (so the user generates one and watches the balance change).
        if (!await db.Fees.AnyAsync())
        {
            db.Fees.AddRange(new[]
            {
                new Fee { Name = "Tuition", Amount = 25000m, IsActive = true },
                new Fee { Name = "Miscellaneous", Amount = 3500m, IsActive = true }
            });
            await db.SaveChangesAsync();
        }

        // Module 6 — Accounting demo data: one expense so the ledger shows a negative move out.
        if (!await db.Expenses.AnyAsync())
        {
            db.Expenses.Add(new Expense
            {
                Description = "School supplies",
                Category = "Supplies",
                Amount = 5000m,
                Date = DateTime.Today.AddDays(-3)
            });
            await db.SaveChangesAsync();
        }

        var activeYear = await db.SchoolYears.FirstOrDefaultAsync(y => y.IsActive);
        var juan = await db.Students.FirstOrDefaultAsync(s => s.LastName == "Dela Cruz");
        var ana = await db.Faculty.FirstOrDefaultAsync(f => f.Email == "teacher@educore.local");

        // Module 2 — Enrollment demo data: Juan applying to Grade 1, still pending approval.
        if (activeYear is not null && juan is not null && !await db.Enrollments.AnyAsync())
        {
            db.Enrollments.Add(new Enrollment
            {
                StudentId = juan.Id,
                SchoolYearId = activeYear.Id,
                GradeLevelId = 1,
                Status = EnrollmentStatus.Applied,
                ApplicationDate = DateTime.Today.AddDays(-5)
            });
            await db.SaveChangesAsync();
        }

        if (activeYear is not null && !await db.Sections.AnyAsync())
        {
            var section = new Section
            {
                Name = "Grade 1 - A",
                GradeLevelId = 1,
                SchoolYearId = activeYear.Id,
                AdviserId = ana?.Id
            };
            db.Sections.Add(section);
            await db.SaveChangesAsync();

            if (juan is not null && juan.SectionId is null)
            {
                juan.SectionId = section.Id;
                await db.SaveChangesAsync();
            }

            if (ana is not null)
            {
                var math = await db.Subjects.FirstOrDefaultAsync(s => s.Code == "MATH");
                var eng = await db.Subjects.FirstOrDefaultAsync(s => s.Code == "ENG");
                if (math is not null)
                    db.SectionSubjects.Add(new SectionSubject { SectionId = section.Id, SubjectId = math.Id, FacultyId = ana.Id });
                if (eng is not null)
                    db.SectionSubjects.Add(new SectionSubject { SectionId = section.Id, SubjectId = eng.Id, FacultyId = ana.Id });
                await db.SaveChangesAsync();
            }
        }

        // Module 7 — a seeded teacher→parent message about Juan, so the inbox has content.
        if (ana?.ApplicationUserId is not null && juan?.ApplicationUserId is not null && !await db.Messages.AnyAsync())
        {
            db.Messages.Add(new Message
            {
                SenderId = ana.ApplicationUserId,
                RecipientId = juan.ApplicationUserId,
                StudentId = juan.Id,
                Body = "Welcome to Grade 1-A! Reach me through this portal anytime.",
                SentAt = DateTime.UtcNow.AddDays(-1),
                Seen = false
            });
            await db.SaveChangesAsync();
        }
    }
}