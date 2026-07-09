# Software Requirements Specification (SRS)
## Project: ReferWell — Referral Triage & SLA Queue Management

---

## 1. Introduction

### 1.1 Purpose
This document details the functional and non-functional requirements for the ReferWell system. ReferWell is a clinical referral management platform built to orchestrate specialist care referrals, enforce contractual Service Level Agreements (SLAs), ensure multi-user safety via optimistic concurrency safeguards, and facilitate throttled bulk patient messaging.

### 1.2 Scope
The scope of ReferWell covers:
1.  **A .NET 8.0 Web API backend** following Clean Architecture principles.
2.  **A Next.js (App Router, TypeScript) frontend** styled with Tailwind CSS, utilizing a shared top navigation menu layout.
3.  **Entity Framework Core mapping to Microsoft SQL Server** (`MUHAMMADUMARN\SQLEXPRESS`) with Windows Authentication.
4.  **Role-Based Access Control (RBAC)** across three specific user classes: Admin, Triage Nurse, and General Practitioner (GP).
5.  **A throttled mass-communication service** utilizing memory channels.

### 1.3 Definitions, Acronyms, and Abbreviations
*   **GP**: General Practitioner (creator of referrals).
*   **SLA**: Service Level Agreement (a binding deadline for care triage).
*   **RBAC**: Role-Based Access Control.
*   **JWT**: JSON Web Token (session authorization).
*   **Concurrency Token**: A database field (`rowversion`) tracking modifications to prevent overwrite races.

---

## 2. Overall Description

### 2.1 Product Perspective
ReferWell replaces fragmented legacy databases and spreadsheets. It operates as a web-based client-server system that uses persistent SignalR connections to sync the referral queue across all open browser sessions in real-time.

### 2.2 Product Functions
*   Secure Login and Role Resolution.
*   New Referral Entry (GP-only).
*   Active Referral Queue (ordered dynamically by Priority Score).
*   Referral Claim/Release Lockout.
*   Referral Lifecycle State Transitioning.
*   Dynamic Weight Configuration Sliders.
*   Templated Mass Communication Filtering and Queue Throttling.
*   User Profile Management (Admin-only).

### 2.3 User Classes and Characteristics
1.  **Admin**: Manages system users. Can view all active referrals. Configures the urgency, wait time, and patient age weights in the prioritization formula.
2.  **Triage Nurse**: Performs primary triage. Claims referrals, transitions their status along the state machine, and drafts/sends bulk communication campaigns.
3.  **GP (General Practitioner)**: Submits patient referrals. Can only see referrals they created (filtered view). Has no access to configurations, user management, or mass communications.

### 2.4 Operating Environment
*   **Client**: Modern web browsers (Chrome, Edge, Firefox, Safari) running Next.js.
*   **Server**: .NET Core Web API running on Kestrel.
*   **Database**: Microsoft SQL Server.

### 2.5 Design and Implementation Constraints
*   Must compile under .NET 8.0 SDK.
*   Frontend must use Next.js App Router and Tailwind CSS.
*   Database must use MS SQL Server (no SQLite).
*   Integrated security/Windows Auth must be supported for the DB.

---

## 3. Functional Requirements

### 3.1 Authentication & Session Management
*   **FR-1.1**: The system must verify credentials using a secure Login page.
*   **FR-1.2**: Passwords must be hashed using the BCrypt algorithm.
*   **FR-1.3**: Upon successful verification, the system must return a JSON Web Token (JWT) containing user ID, email, name, and role claims.
*   **FR-1.4**: Client applications must attach the JWT bearer token to the `Authorization` header of all subsequent API calls.
*   **FR-1.5**: Sessions must support safe sign-out by purging JWT tokens from client local storage.

### 3.2 Referral Submission & State Machine
*   **FR-2.1**: GPs must be able to submit referrals containing: Patient Name, Patient DOB, Specialist Type, Reason for Referral, and Urgency Level.
*   **FR-2.2**: The system must enforce the following states:
    `Received -> Triaged -> Accepted/Declined -> Booked -> Completed`
*   **FR-2.3**: Transitions must validate against allowed paths:
    *   `Received` can transition *only* to `Triaged`.
    *   `Triaged` can transition to `Accepted` or `Declined`.
    *   `Accepted` can transition *only* to `Booked`.
    *   `Booked` can transition *only* to `Completed`.
*   **FR-2.4**: The system must calculate the SLA deadline automatically upon receipt:
    *   **Emergency**: 4 hours.
    *   **Urgent**: 24 hours.
    *   **Soon**: 7 days.
    *   **Routine**: 30 days.

### 3.3 Dynamic Prioritization Configuration
*   **FR-3.1**: Triage scores must be calculated using a weighted formula:
    $$\text{Score} = (W_{\text{urgency}} \times U) + (W_{\text{waittime}} \times T) + (W_{\text{patient}} \times P)$$
*   **FR-3.2**: Admins must be able to adjust weight percentages via sliders.
*   **FR-3.3**: The weights must sum to exactly 100% before they can be saved.
*   **FR-3.4**: Saving new weights must trigger an instant recalculation of all active referral scores and resort the queue.

### 3.4 Multi-User Concurrency Claims
*   **FR-4.1**: A triage nurse must claim a referral to gain exclusive rights to transition its status.
*   **FR-4.2**: Multiple nurses must be prevented from claiming or updating the same referral concurrently.
*   **FR-4.3**: Database rows must track a `rowversion` column. If a nurse attempts to modify a referral whose `rowversion` has changed, the API must return a `409 Conflict` response.
*   **FR-4.4**: The client application must render a prominent error toast informing the user that the referral was modified by another session and prompt a refresh.

### 3.5 Real-Time Queue & Live Sync
*   **FR-5.1**: The frontend dashboard must connect to the backend SignalR Queue Hub on load.
*   **FR-5.2**: The SignalR Hub must broadcast events when a referral is created, claimed, released, transitioned, or weights are resorted.
*   **FR-5.3**: Receiving a hub broadcast must trigger an automatic, silent background refresh of the active queue view for all connected users.

### 3.6 Mass Communications Throttled Module
*   **FR-6.1**: Nurses/Admins must be able to create campaigns, filtering recipients by status or urgency.
*   **FR-6.2**: The email template must interpolate merge fields: `{PatientName}`, `{SpecialistType}`, `{Status}`, and `{SlaDeadline}`.
*   **FR-6.3**: Outgoing messages must be enqueued into an in-memory queue channel.
*   **FR-6.4**: A background service must process the channel at a throttled rate of 2 messages per second.

### 3.7 User Administration
*   **FR-7.1**: Admins must have full access to User Management to create new users, edit details, and deactivate accounts.

---

## 4. Non-Functional Requirements

### 4.1 Security Baseline (OWASP Compliance)
*   **NFR-1.1**: Direct database queries are prohibited. All database communication must run through EF Core parameterized queries to prevent SQL Injection.
*   **NFR-1.2**: APIs must reject unauthorized calls by decorating controllers/actions with `[Authorize(Roles = "...")]` attributes.
*   **NFR-1.3**: Presentation layouts must hide navigational elements (e.g. User Management links) for roles lacking matching permissions.
*   **NFR-1.4**: Security headers must be sent on all responses:
    *   `X-Frame-Options: DENY` (Clickjacking prevention).
    *   `X-Content-Type-Options: nosniff` (MIME sniffing prevention).
    *   `X-XSS-Protection: 1; mode=block` (Cross-Site Scripting protection).

### 4.2 Usability & Styling Controls
*   **NFR-2.1**: The layout must follow a professional, modern dark-theme aesthetic using glassmorphism and Tailwind CSS grids.
*   **NFR-2.2**: The application must utilize a top navigation bar header layout, maintaining visual layout consistency for all user roles.

### 4.3 Performance & Reliability
*   **NFR-3.1**: SLA countdown timers must refresh on the client-side every 1000ms using ticking interval cycles.
*   **NFR-3.2**: Throttled messaging workers must operate independently without blocking the Web API thread pools.
