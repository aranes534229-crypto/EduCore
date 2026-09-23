# EduCore

EduCore is a greenfield ERP + CRM system designed for private schools. It consolidates admissions, enrollment, billing, faculty, scheduling, accounting, and parent‑teacher communication into a single platform with strict role‑based access control.

## Core Modules

1. **Student Information Management** – Full student records including personal, guardian, and document data.
2. **Enrollment Management** – Applications, approvals/rejections, and school‑year configuration.
3. **Tuition & Billing** – Fees, discounts, payment plans, and transaction tracking.
4. **Faculty Management** – Teacher records and related operations.
5. **Class Scheduling** – Sections, rooms, timeslots, and student assignment.
6. **Accounting & Expenses** – Income, expenses, and budgeting.
7. **Parent/Student CRM** – Messaging, announcements, and notifications.
8. **Inquiry & Admission Tracking** – Lead management and staff assignment.
9. **Reports & Analytics** – Role‑scoped reporting and analytics.

## Roles & Permissions

| Role | Scope |
|------|-------|
| **Admin** | Full control |
| **Registrar** | Inquiry → enrollment → class assignment (no money access) |
| **Faculty** | Own classes/students – grades, attendance, parent messages |
| **Finance** | Money‑only – billing, payments, expenses, money reports |
| **Parent/Student** | Own or child’s record – apply, view schedule, pay, message teachers |

## Workflow Overview

Inquiry → follow‑up → application → student record → enrollment approve → class assignment → billing → payment → payment confirm → classes begin (grades/attendance) → parent communication → expense tracking → reporting.

## Technology Stack

- **Backend** – ASP.NET Core MVC
- **ORM** – Entity Framework Core
- **Database** – SQL Server
- **Frontend** – Razor Views with optional jQuery or vanilla JS for interactivity

## Getting Started

```bash
# build

dotnet build
# database migration

dotnet ef database update
# run

dotnet run --project src/EduCore.csproj
```

Default URL: `http://localhost:5000` (HTTP) / `https://localhost:5001` (HTTPS).

---

This readme provides a quick snapshot of the system’s purpose, modules, roles, workflow, and stack. Happy coding!