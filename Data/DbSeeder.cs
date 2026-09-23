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
                StudentNumber = "1001",
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

        // Module 7 — link students to the demo Parent login so the portal shows billing/messaging
        // for every enrolled child, not just Juan. Re-runs on every start, so students created
        // through the UI (or converted from inquiries) get picked up the next time the app boots.
        // Real deployment would link each student to their own guardian account instead.
        var parentUser = await users.FindByEmailAsync("parent@educore.local");
        if (parentUser is not null)
        {
            var unlinked = await db.Students.Where(s => s.ApplicationUserId == null).ToListAsync();
            if (unlinked.Count > 0)
            {
                foreach (var s in unlinked) s.ApplicationUserId = parentUser.Id;
                await db.SaveChangesAsync();
                Console.WriteLine($"[SEED] Linked {unlinked.Count} student(s) to parent {parentUser.DisplayName}.");
            }
        }

// Module 8 — one lead inquiry, assigned to the Registrar for follow-up.
            var regUser = await users.FindByEmailAsync("reg@educore.local");
            Console.WriteLine($"[SEED] regUser found: {regUser?.DisplayName}");
            Console.WriteLine($"[SEED] parentUser found: {parentUser?.DisplayName}");
            Console.WriteLine($"[SEED] Inquiries already exist: {await db.Inquiries.AnyAsync()}");
            if (regUser is not null && !await db.Inquiries.AnyAsync())
            {
                Console.WriteLine("[SEED] Inserting inquiries...");
                db.Inquiries.Add(new Inquiry
                {
                    StudentName = "Karla Reyes",
                    GradeLevelId = 1,
                    ContactEmail = "karla.reyes@example.com",
                    ContactPhone = "0919-000-0000",
                    InternalNotes = "Walk-in visit; interested in Grade 1.",
                    Source = "Walk-in",
                    Type = "Admissions",
                    AssignedToId = regUser.Id,
                    Status = InquiryStatus.New,
                    DateCreated = DateTime.Today.AddDays(-1)
                });
                await db.SaveChangesAsync();

                // Parent-submitted inquiry (logged in as Maria Dela Cruz) for testing the portal.
                db.Inquiries.Add(new Inquiry
                {
                    StudentName = "Sofia Dela Cruz",
                    GradeLevelId = 1,
                    ContactEmail = "parent@educore.local",
                    ContactPhone = "0917-123-4567",
                    Source = "Website",
                    Type = "Admissions",
                    CreatedByUserId = parentUser?.Id,
                    Status = InquiryStatus.New,
                    DateCreated = DateTime.Today.AddDays(-2)
                });
                await db.SaveChangesAsync();
                Console.WriteLine("[SEED] Inquiries saved.");

                // Add the parent's question as the first thread note.
                var sofiaInquiry = await db.Inquiries.FirstOrDefaultAsync(i => i.StudentName == "Sofia Dela Cruz");
                Console.WriteLine($"[SEED] Sofia found: {sofiaInquiry?.Id}");
                if (sofiaInquiry is not null)
                {
                    var note = new InquiryNote
                    {
                        InquiryId = sofiaInquiry.Id,
                        StaffId = null,
                        Visibility = "Parent",
                        Body = "New inquiry submitted — awaiting registrar follow-up.",
                        Timestamp = sofiaInquiry.DateCreated
                    };
                    Console.WriteLine($"[SEED] Adding note for inquiry {note.InquiryId}, body: {note.Body}");
                    db.Entry(note).State = EntityState.Added;
                    var result = await db.SaveChangesAsync();
                    Console.WriteLine($"[SEED] SaveChanges result: {result}");
                }

                // Registrar follow-up reply to demonstrate the conversation thread.
                if (sofiaInquiry is not null && regUser is not null)
                {
                    sofiaInquiry.AssignedToId = regUser.Id;
                    sofiaInquiry.FirstResponseAt = DateTime.UtcNow.AddDays(-1);
                    db.InquiryNotes.Add(new InquiryNote
                    {
                        InquiryId = sofiaInquiry.Id,
                        StaffId = regUser.Id,
                        Visibility = "Parent",
                        Body = "Thank you for your inquiry! We do have open slots in Grade 1-A for the 2025-2026 school year. Please schedule a campus visit by booking through our portal.",
                        Timestamp = DateTime.UtcNow.AddDays(-1)
                    });
                    await db.SaveChangesAsync();
                    Console.WriteLine($"[SEED] Follow-up note saved. Count after: {await db.InquiryNotes.CountAsync()}");
                }
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
        if (!await db.Fees.AnyAsync(f => f.GradeLevelId.HasValue))
        {
            var gradeLevels = await db.GradeLevels.OrderBy(g => g.SortOrder).ToListAsync();
            db.Fees.AddRange(gradeLevels.Select(g => new Fee
            {
                Name = $"{g.Name} Tuition",
                Amount = g.Amount,
                IsActive = true,
                GradeLevelId = g.Id
            }));
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

        // Module 3 — seed one invoice + partial payment for Juan so the parent portal shows the
        // harmonized Tuition & Billing layout (status badge, payment history, account-only payment)
        // on first login instead of the empty state. Guard per-student (not "no invoices at all") so
        // the demo is present even when the DB already holds other students' statements.
        if (activeYear is not null && juan is not null && !await db.Invoices.AnyAsync(i => i.StudentId == juan.Id))
        {
            var inv = new Invoice
            {
                Number = await InvoiceNumbering.NextAsync(db),
                StudentId = juan.Id,
                SchoolYearId = activeYear.Id,
                IssuedDate = DateTime.Today.AddDays(-7),
                Lines =
                {
                    new InvoiceLine { Description = "Tuition", Amount = 25000m },
                    new InvoiceLine { Description = "Miscellaneous", Amount = 3500m }
                }
            };
            db.Invoices.Add(inv);
            await db.SaveChangesAsync();
            db.Payments.Add(new Payment
            {
                InvoiceId = inv.Id,
                Amount = 10000m,
                Method = PaymentMethods.ParentOnly,
                Reference = "Parent portal — partial",
                Date = DateTime.Today.AddDays(-3)
            });
            await db.SaveChangesAsync();
        }

        // Module 2 — Enrollment demo data: Juan enrolled; he's placed in a section below, so his
        // enrollment reads Scheduled. Luis & Miguel (below) are left Unscheduled for the pool.
        if (activeYear is not null && juan is not null && !await db.Enrollments.AnyAsync())
        {
            db.Enrollments.Add(new Enrollment
            {
                StudentId = juan.Id,
                SchoolYearId = activeYear.Id,
                GradeLevelId = 1,
                Status = EnrollmentStatus.Scheduled,
                ApplicationDate = DateTime.Today.AddDays(-5)
            });
            await db.SaveChangesAsync();
        }

        // Module 5 — an enrolled student who is NOT yet placed in a section, so the
        // Registrar has someone to schedule (unscheduled pool) and hand to Finance.
        if (activeYear is not null && !await db.Students.AnyAsync(s => s.LastName == "Ramos"))
        {
            var luis = new Student
            {
                StudentNumber = "1002",
                FirstName = "Luis",
                LastName = "Ramos",
                GuardianName = "Nena Ramos",
                GuardianContact = "0917-000-1111"
            };
            db.Students.Add(luis);
            await db.SaveChangesAsync();
            db.Enrollments.Add(new Enrollment
            {
                StudentId = luis.Id,
                SchoolYearId = activeYear.Id,
                GradeLevelId = 1,
                ApplicationDate = DateTime.Today.AddDays(-3)
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

        // Module 5 — second grade so the Schedule grade filter can be verified across grades.
        // Mirrors the Grade 1-A block above: a Grade 2 section + one unplaced student with an
        // Unscheduled Grade 2 enrollment. Opening the Grade 2 Schedule page should list Miguel only —
        // Luis (an unplaced, Unscheduled Grade 1 student) must be excluded, proving the pool branches by grade.
        if (activeYear is not null && !await db.Students.AnyAsync(s => s.LastName == "Santos"))
        {
            var miguel = new Student
            {
                StudentNumber = "1003",
                FirstName = "Miguel",
                LastName = "Santos",
                GuardianName = "Lola Santos",
                GuardianContact = "0917-000-2222"
            };
            db.Students.Add(miguel);
            await db.SaveChangesAsync();
            db.Enrollments.Add(new Enrollment
            {
                StudentId = miguel.Id,
                SchoolYearId = activeYear.Id,
                GradeLevelId = 2,
                ApplicationDate = DateTime.Today.AddDays(-4)
            });
            await db.SaveChangesAsync();
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

        // ponytail: fixed admission IDs keyed by last name — keeps the demo student list readable
        // across DB states (seeds + runtime-created Angelo). Forked into per-lead custom IDs later
        // if demos ever carry real PII. This also frees the count-based generator to start fresh.
        var demoNumbers = new (string Last, int Seq)[]
        {
            ("Dela Cruz", 1), ("Ramos", 2), ("Santos", 3), ("Ranes", 4)
        };
        foreach (var (last, seq) in demoNumbers)
        {
            var st = await db.Students.FirstOrDefaultAsync(s => s.LastName == last);
            var want = $"{1000 + seq:D4}";
            if (st is not null && st.StudentNumber != want)
            {
                st.StudentNumber = want;
                await db.SaveChangesAsync();
            }
        }
    }
}