# Release Notes - ReferWell v1.2.1

This release implements significant refinements to the Referral Triage and Queue Workflow. It secures referral access by partitioning views based on roles, ensures that referrals are submitted to a shared hospital queue as unassigned, and restricts assignee capabilities to hospital staff (Admin/Triage Nurse).

---

## Key Features & Refinements

### 1. Secured Queue & Referral Visibility
*   **GP Role Restriction**: Referring GPs can now only see and search for referrals that they created (`CreatedByUserId`), preventing them from accessing or searching other GPs' referrals.
*   **Shared Hospital Triage Queue**: Admins and Triage Nurses retain complete access to all referrals in the shared triage queue.
*   **Access Check Guards**: Integrated access checks (`CanAccessReferral`) in `ReferralService` to explicitly forbid GPs from retrieving, editing, transitioning, or claiming referrals they did not create.

### 2. Auto-Assignment & Assignee Filtering
*   **Initial Unassigned Queue**: New referrals created by GPs are submitted to the shared hospital queue as `Unassigned`, rather than being automatically assigned to the GP.
*   **Claiming Auto-Assignment**: When a Triage Nurse or Admin claims a referral, the system now automatically updates `AssignedToUserId` to the claiming user.
*   **Filtered Assignees**: Restructured the frontend dashboard filters and edit screens to only show hospital staff (Admins and Triage Nurses) in the Assignee dropdown list, ensuring that referring GPs cannot be assigned to triage tickets.

### 3. Database Seeding & Migrations
*   **Stable Seed Referrals**: Updated EF Core data seeding to use stable GUIDs for referral records and ensure correct, production-like assignee values (either unassigned or assigned to specific triage nurses/admins).
*   **Shared Triage Migration**: Added a new database migration (`SharedQueueVisibilityAndNurse2`) representing the updated seeding configuration.

---

## Technical Audit & Verification

### C# Backend Unit Tests
All 39 backend tests compile and pass cleanly:
```bash
Passed!  - Failed:     0, Passed:    39, Skipped:     0, Total:    39, Duration: 44 ms - ReferWell.Tests.dll (net8.0)
```

---

# Release Notes - ReferWell v1.2.0

This release introduces a clean architecture refactoring of the backend API, moving core business logic and database orchestration into the `ReferWell.Application` layer. It also establishes a comprehensive, safe, and HIPAA-compliant request logging framework on both the ASP.NET Core backend and Next.js frontend, ensuring zero exposure of credentials, authorization tokens, request bodies, or Protected Health Information (PHI).

---

## Key Features & Refinements

### 1. Clean Architecture Refactoring
*   **Decoupled Controllers**: API controllers in `ReferWell.Api` are now lightweight entrypoints that delegate work to the application layer.
*   **Application Services & Logic**: Core logic is moved to `ReferWell.Application` services, queries, and commands, enhancing testability, reuse, and single-responsibility principles.
*   **Unified Application Setup**: Registered application dependencies dynamically via `DependencyInjection.cs` extension methods.

### 2. ASP.NET Core Backend Request Logging
*   **Security-First Middleware**: Implemented `RequestLoggingMiddleware` to capture HTTP methods, sanitized paths, HTTP status codes, execution durations, user IDs, and trace IDs.
*   **Path & Query Sanitization**: Built `RequestLogSanitizer` to automatically redact sensitive query string parameters (`access_token`, `token`, `password`, `authorization`, `api_key`, `secret`).
*   **No Payload Exposure**: Explicitly avoids logging request/response bodies, HTTP authorization headers, JWTs, and PHI payloads.
*   **Asynchronous File Logging**: Configured a `RequestFileLogger` that enqueues log lines and flushes them to daily files (e.g., `requests-yyyy-MM-dd.log`) under `backend/logs/` asynchronously using a lock-free concurrent queue mechanism.

### 3. Next.js Frontend Request Logging
*   **API Wrapper Hook**: Integrated `apiFetch` inside `frontend/lib/api.ts` to automatically intercept frontend API calls, calculate request durations, and log sanitized request metadata.
*   **Client Log Redaction**: Redacts query parameters before any logging or transmission using a frontend URL sanitizer.
*   **Server-Side Log Persistence**: Developed a secure API route (`/api/request-log`) that accepts browser-originated request logs and writes them to dated logs under `frontend/logs/` while enforcing strict validation rules on incoming methods, path sizes, and statuses.

### 4. Code Quality & Test Expansion
*   **Expanded Unit Tests**: Increased C# backend unit tests to **39 passing tests** validating authentication, configurations, patient processing, user roles, menu access, and the SLA clock state machine.
*   **Zero-Error Frontend Compilation**: Successfully verified the Next.js production build (`npm run build`) with zero linting or TypeScript compilation warnings.

---

## Technical Audit & Verification

### C# Backend Unit Tests
All 39 backend tests compile and pass cleanly:
```bash
Passed!  - Failed:     0, Passed:    39, Skipped:     0, Total:    39, Duration: 41 ms - ReferWell.Tests.dll (net8.0)
```

---

# Release Notes - ReferWell v1.1.0

ReferWell is a referral management and triage system. This release introduces advanced referral grid refining, SLA timer controls (SLA pausing and resuming), refined mass communications, dynamic menu-based authorization, security enhancements, and robust test coverage.

---

## Key Features & Refinements

### 1. Referral Grid & Queue Filtering
*   **Granular Filters**: The dashboard referral queue can now be filtered by:
    *   **Urgency Levels** (Routine, Semi-Urgent, Urgent)
    *   **Status** (Received, Triaged, Accepted, Declined, Booked, Completed)
    *   **Specialist Types**
    *   **Assignees** (Assigned Clinicians/GPs)
    *   **SLA Status** (Only show SLA-breached referrals)
    *   **Submission Date Range** (From/To)
    *   **Case Number Search**
*   **Searchable Dropdowns**: Implemented a reusable, searchable multi-select filter component (`FilterGroup`) to handle clean, quick selection on large datasets.
*   **Urgency Cleanup**: Refactored urgency levels to represent standard categories (**Routine**, **Semi-Urgent**, **Urgent**), mapping them accurately in database seeds, UI forms, and tests.

### 2. SLA Countdown & Clock Controls
*   **Live SLA Timer**: Added a real-time countdown timer (`SlaTimer` component) showing exact days, hours, or minutes remaining/overdue, including automatic color changes and warning states.
*   **SLA Clock Pausing & Resuming**:
    *   Authorized users (Admin, Triage Nurse, and GP) can freeze the SLA clock when a referral is waiting on patient actions/information.
    *   Resuming the SLA clock automatically calculates the paused duration and extends the SLA deadline by the exact time elapsed.
    *   Closed/completed referrals automatically discard any active pause state.
*   **Automated Breach Scanner**: Implemented `SlaBreachBackgroundService` in the backend which checks for overdue Received referrals every minute, updates their SLA status in the database, and pushes real-time SignalR alerts to the queue.

### 3. Refined Mass Communications System
*   **Targeted Campaigns**: Campaigns can now target patients or referring GPs, filtering recipients with the exact same filters as the main Referral Grid.
*   **Smart Template Validation**: Restricts body and subject templates to supported merge fields: `{PatientName}`, `{CaseNo}`, `{SpecialistType}`, `{Status}`, `{Urgency}`, `{SlaDeadline}`, `{ReceivedDate}`, `{ReferringGPName}`.
*   **Recipient Previews**: Previews up to the first 100 resolved recipients with dynamic variables evaluated before confirmation.
*   **Enhanced Audit Tracking**:
    *   Background service logs exact SMTP delivery statuses ("Sent" or "Failed" along with C# exception messages).
    *   Clicking on any completed campaign in the UI launches a modal displaying the full list of sent messages, recipient types, and delivery logs.

### 4. Dynamic Menu Authorization
*   **Dynamic Role-Menu Mapping**: Replaced hardcoded controller authorizations (`[Authorize(Roles = "...")]`) with a dynamic filter `[MenuAuthorize("Menu Name")]`. This evaluates permissions based on database-configured `RoleMenuAccess` entries.
*   **Secured Routing**: Next.js client routes and navigation links are built and guarded dynamically based on permissions loaded from the backend API.

### 5. Security & Framework Enhancements
*   **Password Hashing**: Removed plain-text password properties from the database schema, models, and responses. All passwords are now handled strictly via BCrypt hashes.
*   **Timezone Consistency**: Standardized dates to use local time (`DateTime.Now` / `DateTimeKind.Local`) across domain services, database context seeds, and controller operations to eliminate timezone offsets.
*   **Role Management Fix**: Refactored role update collection handling in `UsersController` to explicitly track role entity removals/additions, avoiding Entity Framework tracking collision issues.

---

## Technical Audit & Verification

### C# Backend Unit Tests
A suite of 19 unit tests validates the domain logic, including:
*   Priority weight scoring calculations.
*   SLA deadline resolution for each urgency level.
*   SLA breach evaluation.
*   SLA pausing clock freezing and breach exemption.
*   SLA resuming deadline extensions.
*   SLA pause state clearing on status transition.

All 19 tests compile and pass cleanly:
```bash
Test run for D:\AIProjects\ReferWell\backend\ReferWell.Tests\bin\Debug\net8.0\ReferWell.Tests.dll
Passed!  - Failed:     0, Passed:    19, Skipped:     0, Total:    19, Duration: 39 ms
```
