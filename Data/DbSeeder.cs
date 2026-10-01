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

        

        if (!await db.Faculty.AnyAsync())
        {
            var person = new Person
            {
                FirstName = "Ana",
                LastName = "Santos",
                Email = "teacher@educore.local",
                Phone = "0918-000-0000",
            };
            db.Persons.Add(person);
            await db.SaveChangesAsync();

            var teacher = new Faculty
            {
                EmployeeNumber = "EMP-001",
                HireDate = DateTime.Today.AddYears(-2),
                IsActive = true,
                PersonId = person.Id
            };
            db.Faculty.Add(teacher);

            // Give the demo teacher a login in the Faculty role so the portal is testable.
            if ((await users.FindByEmailAsync(person.Email)) is null)
            {
                var acct = new ApplicationUser
                {
                    UserName = person.Email,
                    Email = person.Email,
                    PersonId = person.Id,
                    EmailConfirmed = true
                };
                var result = await users.CreateAsync(acct, "Teacher123!");
                if (result.Succeeded)
                {
                    await users.AddToRoleAsync(acct, AppRoles.Faculty);
                }
            }
        }

        await db.SaveChangesAsync();

        // Module 7 — link the demo Parent login to their Person so the portal shows
        // billing/messaging for their child. Student ↔ Person is one-to-one, so a parent
        // account maps to exactly one student record; re-runs on every start.
        // Real deployment would link each student to their own guardian account instead.
        var parentUser = await users.FindByEmailAsync("parent@educore.local");
        if (parentUser is not null && parentUser.PersonId is null)
        {
            var parentPerson = await db.Persons.FirstOrDefaultAsync(p => p.Email == parentUser.Email);
            if (parentPerson is null)
            {
                parentPerson = new Person { FirstName = "Demo", LastName = "Parent", Email = parentUser.Email ?? "" };
                db.Persons.Add(parentPerson);
                await db.SaveChangesAsync();
            }
            parentUser.PersonId = parentPerson.Id;
            await users.UpdateAsync(parentUser);
        }

        // Self-heal duplicate StudentNumbers — count-based generators minted duplicates when
        // rows were deleted or the table truncated. The higher Id keeps its number; earlier
        // duplicates get the next free number (max + 1). Re-runs on every start.
        var allNumbers = await db.Students
            .Where(s => s.StudentNumber != null)
            .Select(s => new { s.Id, Number = s.StudentNumber! })
            .ToListAsync();
        var dupIds = allNumbers
            .GroupBy(s => s.Number)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.OrderBy(x => x.Id).Skip(1).Select(x => x.Id))
            .ToList();
        if (dupIds.Count > 0)
        {
            var max = allNumbers.Select(n => int.TryParse(n.Number, out var v) ? v : 0).ToList();
            var next = (max.Count > 0 ? max.Max() : 1000) + 1;
            var dupes = await db.Students.Where(s => dupIds.Contains(s.Id)).ToListAsync();
            foreach (var s in dupes.OrderBy(s => s.Id))
            {
                Console.WriteLine($"[SEED] Renumbered duplicate student {s.Id}: {s.StudentNumber} -> {next:D4}");
                s.StudentNumber = $"{next:D4}";
                next++;
            }
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

        // Module 3 — Tuition & Billing demo data: one master tuition Fee per grade level
        // (linked via GradeLevelId), so scheduling auto-billing has a fee to find.
        // Misc/Activity fees are shared rows with GradeLevelId null (created manually later).
        // ponytail: EnsureExistingFeesHaveLines — backfill FeeLine rows for Fees created
        // before the line-item refactor (they have no Lines yet). Idempotent: skips fees
        // that already have active lines.
        var gradeLevels = await db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync();
        if (!await db.Fees.AnyAsync(f => f.GradeLevelId.HasValue))
        {
            db.Fees.AddRange(gradeLevels.Select(g => new Fee
            {
                Name = $"{g.Name} Tuition",
                IsActive = true,
                GradeLevelId = g.Id,
                Lines = new List<FeeLine>
                {
                    new FeeLine { Description = "Tuition", Amount = g.Amount, IsActive = true }
                }
            }));
            await db.SaveChangesAsync();
        }
        else
        {
            // Backfill: any existing Fee with a GradeLevelId but no active lines gets seeded from GradeLevel.Amount.
            var feesNeedingLines = await db.Fees
                .Include(f => f.Lines)
                .Where(f => f.GradeLevelId.HasValue && !f.Lines.Any(l => l.IsActive))
                .ToListAsync();
            foreach (var fee in feesNeedingLines)
            {
                var gradeAmount = gradeLevels.FirstOrDefault(g => g.Id == fee.GradeLevelId)?.Amount ?? 0m;
                fee.Lines.Add(new FeeLine { Description = "Tuition", Amount = gradeAmount, IsActive = true });
            }
            if (feesNeedingLines.Any()) await db.SaveChangesAsync();
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
        var ana = await db.Faculty.Include(f => f.Person).FirstOrDefaultAsync(f => f.Person!.Email == "teacher@educore.local");

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

        if (activeYear is not null && !await db.Sections.AnyAsync(s => s.GradeLevelId == 2 && s.SchoolYearId == activeYear.Id))
        {
            db.Sections.Add(new Section
            {
                Name = "Grade 2 - A",
                GradeLevelId = 2,
                SchoolYearId = activeYear.Id,
                AdviserId = ana?.Id
            });
            await db.SaveChangesAsync();
        }

        // Numbering is owned by the max-based generators (StudentsController/InquiriesController)
        // plus the dedupe pass above — no forced demo numbers here, they used to recreate duplicates.

        // Backfill: ensure every user with Faculty role has a Faculty record linked to their Person
        var facultyRole = await roles.FindByNameAsync(AppRoles.Faculty);
        if (facultyRole is not null)
        {
            var facultyUserIds = await db.UserRoles
                .Where(ur => ur.RoleId == facultyRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var facultyUsers = await users.Users
                .Include(u => u.Person)
                .Where(u => facultyUserIds.Contains(u.Id))
                .ToListAsync();

            foreach (var u in facultyUsers)
            {
                if (u.PersonId is null)
                {
                    // Create Person from email if missing
                    var email = u.Email ?? "";
                    var parts = email.Split('@')[0].Split('.', '_', '-');
                    var person = new Person
                    {
                        FirstName = parts.FirstOrDefault() ?? "Teacher",
                        LastName = parts.Skip(1).FirstOrDefault() ?? "",
                        Email = email
                    };
                    db.Persons.Add(person);
                    await db.SaveChangesAsync();
                    u.PersonId = person.Id;
                    await users.UpdateAsync(u);
                }

                if (u.PersonId is not null && !await db.Faculty.AnyAsync(f => f.PersonId == u.PersonId))
                {
                    var facultyCount = await db.Faculty.CountAsync();
                    db.Faculty.Add(new Faculty
                    {
                        PersonId = u.PersonId.Value,
                        EmployeeNumber = $"EMP-{1001 + facultyCount:D4}",
                        HireDate = DateTime.Today,
                        IsActive = true
                    });
                    await db.SaveChangesAsync();
                    Console.WriteLine($"[SEED] Backfilled Faculty record for {u.Email}");
                }
            }
        }
    }
}