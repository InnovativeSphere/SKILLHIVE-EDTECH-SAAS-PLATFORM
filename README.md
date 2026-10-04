# SkillHive

A multi-tenant SaaS platform for skill-based education. Any academy can register, subscribe, and use SkillHive to create and sell courses. Students register globally and can enroll in courses from any academy.

Think of it as Udemy, but where any academy — not just individual instructors — can own a branded space, manage their own staff, and sell their own content.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Project Structure](#project-structure)
- [Modules](#modules)
- [API Documentation](#api-documentation)
- [Database Migrations](#database-migrations)
- [Deployment](#deployment)
- [Architecture Notes](#architecture-notes)
- [License](#license)

---

## Overview

SkillHive is a production-grade EdTech SaaS backend built with **ASP.NET Core**, **Entity Framework Core**, and **PostgreSQL**. It supports:

- Multi-tenant academies (structurally separated, never filtered at runtime)
- Course and lesson authoring with lifecycle management
- Quizzes with grading, cooldowns, and attempt limits
- Student enrollment, progress tracking, and certificates
- Reviews and threaded discussions
- Subscription billing with plan-based limits
- Invoices and Paystack-powered payments
- Analytics dashboards for students, academy owners, and platform superadmins
- Append-only audit trail of sensitive actions

The core flow:

1. Academy registers → trial subscription → tenant space
2. Academy invites instructors and moderators
3. Instructors create courses → lessons → materials → quizzes
4. Owner approves → course published
5. Students browse → enroll → learn → pass quizzes → earn certificates

---

## Features

### Authentication & Accounts
- Academy owner registration with OTP email verification
- Student registration with email-link verification
- Unified login for all user types
- Password reset via secure token
- Staff invite flow with set-password links

### Content Authoring
- Courses with full lifecycle (DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED)
- Lessons with ordering and preview mode
- Materials upload to Cloudinary (PDF, DOCX, PPTX, images, video)
- Quizzes attached to course or lesson, with per-question scoring
- Multi-attempt support with cooldowns

### Learning
- Free enrollment (instant) and paid enrollment (payment-gated)
- Per-lesson progress tracking
- Automatic course completion detection
- Certificate generation (PDF + QR code) on completion
- Public certificate verification by verification code

### Engagement
- One review per enrollment, gated on 50% completion
- Threaded comments (2-level), pinned top-level, moderation
- In-app notifications

### Commerce
- Three-tier subscription plans (Starter / Pro / Enterprise)
- Plan-based limits enforced across courses, staff, and students
- Invoice generation (manual and automated)
- Paystack integration for subscription and course purchases
- Webhook verification with HMAC-SHA512
- Idempotent payment processing (safe to retry)

### Platform Operations
- Role-scoped analytics dashboards (student / academy / platform)
- Append-only audit trail
- Superadmin system health and info endpoints

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Framework | ASP.NET Core |
| ORM | Entity Framework Core |
| Database | PostgreSQL |
| Auth | JWT (Bearer + httpOnly cookie bridge) |
| Payments | Paystack |
| File Storage | Cloudinary |
| Email | MailKit + RazorLight templates |
| PDF | QuestPDF + QRCoder |
| Password Hashing | BCrypt.Net-Next |
| API Docs | Swashbuckle (Swagger/OpenAPI) |

---

## Prerequisites

- **.NET 10 SDK** — [download](https://dotnet.microsoft.com/download)
- **PostgreSQL 14+** — locally or hosted
- **Paystack account** (test keys are fine)
- **Cloudinary account**
- **SMTP credentials** (Gmail app password, Mailgun, or any SMTP provider)

---

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd SkillHive

2. Restore dependencies
bash
dotnet restore
3. Configure the application
Copy appsettings.Development.json.example to appsettings.Development.json and fill in the values (see Configuration).

4. Create the database
bash
dotnet ef database update
5. Run the application
bash
dotnet run
The API will be available at http://localhost:5167.

On first run, the database is seeded with:

One superadmin user (superadmin@skillhive.com / SuperAdmin@123)

Global taxonomy (categories and professions)

Three subscription plans (Starter, Pro, Enterprise)

Configuration
All configuration lives in appsettings.json (production) and appsettings.Development.json (local). Real secrets should never be committed.

Required settings
json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=skillhive;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Secret": "A_SECRET_AT_LEAST_32_CHARACTERS_LONG",
    "Issuer": "skillhive",
    "Audience": "skillhiveUsers"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 465,
    "User": "your-email@gmail.com",
    "Pass": "your-app-password",
    "From": "SkillHive <noreply@skillhive.com>"
  },
  "App": {
    "BaseUrl": "http://localhost:5167",
    "Name": "SkillHive"
  },
  "Paystack": {
    "SecretKey": "sk_test_...",
    "PublicKey": "pk_test_...",
    "BaseUrl": "https://api.paystack.co"
  },
  "Cloudinary": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
  }
}

Environment variables in production
For Railway or any other host, override the above using environment variables:

text
ConnectionStrings__DefaultConnection=...
Jwt__Secret=...
Paystack__SecretKey=...
Cloudinary__ApiSecret=...
Double underscore (__) maps to nested JSON keys.

Running the Application
bash
# Development (auto-reload on file change)
dotnet watch run

# Standard run
dotnet run

# Production build
dotnet publish -c Release -o ./publish
The API listens on http://localhost:5167 by default. Swagger is available at /swagger in Development.

Project Structure
text
SkillHive/
├── Common/                  # Cross-cutting utilities
│   ├── ApiResponse.cs       # Standardized API envelope
│   ├── AuditHelper.cs       # Audit action/target constants
│   ├── AnalyticsCache.cs    # In-memory cache wrapper
│   ├── CloudinaryService.cs
│   ├── DateHelper.cs
│   ├── EmailService.cs
│   ├── FileHelper.cs
│   ├── JwtHelper.cs
│   ├── Logger.cs
│   ├── PaginationHelper.cs
│   ├── PaymentHelper.cs
│   ├── PdfService.cs
│   ├── SlugHelper.cs
│   ├── TokenHelper.cs
│   └── Utils.cs
│
├── Data/
│   ├── AppDbContext.cs      # EF Core context + configuration
│   ├── DbSeeder.cs          # Startup seeding
│   └── Seed/                # Individual seeders
│
├── Enums/                   # All enums (grouped by domain)
│
├── Features/                # Feature modules
│   ├── Academies/
│   ├── Analytics/
│   ├── Audit/
│   ├── Auth/
│   ├── Categories/
│   ├── Certificates/
│   ├── Comments/
│   ├── Courses/
│   ├── Email/
│   ├── Enrollments/
│   ├── Invoices/
│   ├── Lessons/
│   ├── Materials/
│   ├── Notifications/
│   ├── Payments/
│   ├── Quizzes/
│   ├── Reviews/
│   ├── Subscriptions/
│   └── Users/
│
├── Middleware/
│   ├── CookieAuthMiddleware.cs
│   └── RawBodyMiddleware.cs
│
├── Migrations/              # EF Core migrations
│
├── Models/                  # Domain entities
│
├── Program.cs               # Application entry point
└── appsettings.json
Every feature follows the same shape:

text
Features/<Name>/
├── Controllers/    # HTTP layer
├── Services/       # Business logic
└── DTOs/           # Request shapes
Modules
SkillHive is organized into 19 modules across 6 phases.

Phase 1 — Foundation
Email — SMTP sender, RazorLight templates

Notifications — in-app + email dispatcher

Auth — registration, login, verification, password reset

Academies — tenant management

Users & Staff — profiles, invites, roles

Phase 2 — Content
Categories & Professions — hybrid global/academy taxonomy

Courses — full lifecycle with owner approval

Lessons — ordered, previewable content

Materials — Cloudinary-backed file uploads

Quizzes — questions, options, attempts, grading

Phase 3 — Learning
Enrollments — progress tracking, completion detection

Certificates — PDF + QR, public verification

Phase 4 — Monetization
Subscriptions & Plans — trial, upgrades, downgrades, limits

Invoices — manual + auto-generated billing records

Payments — Paystack integration with idempotent webhooks

Phase 5 — Engagement
Reviews & Ratings — one per enrollment, 50% gate

Comments & Discussions — threaded, moderated

Phase 6 — Platform Operations
Analytics & Reporting — role-scoped dashboards

Audit Trail & System Admin — append-only log, health checks

API Documentation
Swagger is enabled in Development mode:

text
http://localhost:5167/swagger
Every endpoint includes:

Role requirements (from [Authorize] attributes)

Request body schema (from DTOs)

Query parameter documentation

Response body schemas are not auto-documented (services return anonymous objects). See skillhive-contracts.md for the full request/response reference generated from source.

Database Migrations
bash
# Add a new migration
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update

# List migrations
dotnet ef migrations list

# Roll back to a specific migration
dotnet ef database update <MigrationName>
Important: Always review generated migrations before applying. EF Core can turn what looks like a rename into a DROP COLUMN + ADD COLUMN, which loses data.

Deployment
Railway (recommended for MVP)
Create a new project on Railway

Add a PostgreSQL plugin

Connect your GitHub repository

Set environment variables for all configuration keys

Railway will run dotnet publish and start the app

The start:prod behavior:

bash
dotnet ef database update && dotnet SkillHive.dll
This ensures migrations are applied on every deploy.

Important for production
Never run dotnet ef database update against a shared database without testing the migration locally first

Set ASPNETCORE_ENVIRONMENT=Production

Use HTTPS with valid certificates

Rotate Jwt:Secret and Paystack keys away from test values

Enable forwarded headers if behind a proxy (ForwardedHeadersMiddleware)

Architecture Notes
Multi-tenancy
Academies are separated by foreign keys, not runtime filters. Every query that touches academy data is scoped by academyId — derived from the JWT, never from query parameters. This is enforced at the service layer.

Privacy by design
Superadmins see platform-level counts, not individual user details

Academy owners see their own data

Instructors see their own courses

Students see what they're enrolled in and what's public

Never delete, always preserve
Invoices void (status change, record kept)

Certificates revoke or supersede

Users deactivate

Courses archive

Audit logs append-only

One responsibility per module
Email sends. Notifications decide. Auth authenticates. Courses own courses. Each module knows only what it needs to know.

Denormalization
Counters live on parent records for fast reads:

Course.TotalEnrollments

Course.AverageRating, TotalReviews

Enrollment.ProgressPercentage

They're updated on write, never computed on read.

Plan limit enforcement
Subscription limits are checked before creating resources:

CanAddCourseAsync(academyId)

CanAddStaffAsync(academyId)

CanEnrollStudentAsync(academyId, courseId)

Called from CourseService, StaffService, and EnrollmentService respectively.

Payment idempotency
Paystack webhooks can fire multiple times. Every payment record is keyed by a unique Reference. On webhook receipt:

Look up by reference

If already SUCCESS, return { status: "already_processed" }

Otherwise, process and mark SUCCESS

The VerifyPaymentAsync fallback exists because local development can't receive webhooks — a frontend can call /api/payments/verify after Paystack redirects the user back.

Audit trail
Every sensitive action writes an append-only row to AUDIT_LOGS:

Who (UserId)

Where (AcademyId, IpAddress)

What (Action, TargetType, TargetId)

Context (Metadata as JSON)

Logging is best-effort — a failing audit never breaks the business flow. If the primary operation succeeds and the audit fails, the operation stands.

License
This project is proprietary. All rights reserved.

Built by Salim Sambo.