# EduCore

**ERP + CRM system for private schools** — admissions, enrollment, billing,
faculty, scheduling, accounting, and parent/teacher communication, with strict
role-based access.

## Current status

Greenfield. The only artifact present is `System_Information/EduCore_Proposal.pdf`.

## Stack (inferred / to confirm)

- **Backend:** ASP.NET Core MVC
- **ORM:** EF Core
- **DB:** SQL Server (`RentalSphereDb` files in parent dir suggest SQL Server familiarity)
- **Frontend:** server-rendered Razor Pages/Views (jQuery or vanilla JS for interactivity)

> Run `/init` only once; this file is the source of truth for context going forward.

## Scope — 9 modules (from proposal)

1. **Student Information Management** — full student records (name, birthday, address, guardian info, documents).
2. **Enrollment Management** — applications, approvals/rejections, school-year setup.
3. **Tuition & Billing** — fee amounts, discounts, payment plans, payments received.
4. **Faculty Management** — teacher records.
5. **Class Scheduling** — sections, rooms, time slots, student-to-section assignment.
6. **Accounting & Expenses** — income/expenses, budget.
7. **Parent/Student CRM** — messaging (teacher ↔ parent), announcements.
8. **Inquiry & Admission Tracking** — leads before enrollment, assigning inquiries to staff.
9. **Reports & Analytics** — role-scoped reporting.

## Roles & access (per proposal)

| Role | Scope |
|---|---|
| **Admin** | Everything, full control |
| **Registrar** | Inquiry → enrollment → class assignment. No money access |
| **Faculty** | Own classes/students only — grades, attendance, remarks, message parents |
| **Finance** | Money only — billing, payments, expenses, money reports |
| **Parent/Student** | Own (or child's) record — apply, view schedule, pay, message teachers |

## Lifecycle (from proposal)

inquiry → follow-up → application → student record → enrollment approve →
class assignment → billing → payment → payment confirm → classes begin
(grades/attendance) → parent communication → expense tracking → reporting.

## Project layout (planning)

```
EduCore/
├── src/                      # main app (ASP.NET Core MVC)
│   ├── Controllers/
│   ├── Models/               # EF Core entities (Student, Section, Fee, Invoice, Payment, Inquiry, Message, ...)
│   ├── Data/                 # AppDbContext, EF Core config/seed
│   ├── Services/             # business logic (EnrollmentService, BillingService, SchedulingService, ...)
│   ├── ViewModels/
│   └── Views/                # Razor views
├── tests/                    # unit/integration tests
├── System_Information/       # docs (proposal PDF lives here)
└── CLAUDE.md
```

## Skills & workflow

- **ponytail (full)**: enforced lazy senior dev — no over-building across the 9 modules.
  `stop ponytail` / `normal mode` to disable; `/ponytail lite|full|ultra` to switch.
- **dataviz**: run before any chart in Reports & Analytics. Applies the six checks
  and `validate_palette.js` before shipping a visualization.
- **code-review / simplify**: invoke after each module ships (correctness bugs, then
  reuse/shortest-diff cleanup).
- **update-config**: permissions/hooks live in `.claude/settings.json`, not CLAUDE.md.
- **artifact-design / artifact-diagramming**: apply for UI/UX decisions and
  architecture/data-flow diagrams.

> There is no `web-design-guidelines` skill; `artifact-design` is the design substitute.

## Run

```bash
# build
dotnet build
# database
dotnet ef database update
# run
dotnet run --project src/EduCore.csproj
```

Default URL: `http://localhost:5000` (HTTP) / `https://localhost:5001` (HTTPS).
