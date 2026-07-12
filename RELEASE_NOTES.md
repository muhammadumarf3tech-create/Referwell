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
