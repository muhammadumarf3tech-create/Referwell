# ReferWell — Referral Triage & SLA Queue Management

ReferWell is an enterprise referral triage and SLA queue management system designed for clinical coordinators and General Practitioners (GPs). The application ensures that patient referrals are handled within strict contractual SLA deadlines through real-time queue synchronization, dynamic priority scoring, optimistic concurrency controls for multi-user safety, and a throttled mass-communication alert system.

---

## 🚀 Quick Start (Local Setup)

### Prerequisites
1. **.NET 8.0 SDK**
2. **Node.js (v20+) & npm**
3. **Microsoft SQL Server (LocalDB or SQLEXPRESS)** with Windows Authentication.

### One-Command Startup
ReferWell includes a single PowerShell script that restores dependencies, applies Entity Framework migrations, seeds default users/data, and starts both the backend API and Next.js frontend concurrently.

To run the system, open PowerShell in the project directory and run:
```powershell
.\run.ps1
```

Once running, the system components are available at:
*   **Next.js Frontend**: [http://localhost:4000](http://localhost:4000)
*   **Backend API**: [http://localhost:5165](http://localhost:5165)
*   **Swagger API Docs**: [http://localhost:5165/swagger](http://localhost:5165/swagger)

---

## 👤 Test Credentials
The database seeds default users for testing the Role-Based Access Control (RBAC):

| Role | Email | Password | Permissions Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@referwell.com` | `Admin@123` | Can view all referrals, manage user profiles, modify dynamic priority weights. |
| **Triage Nurse** | `nurse@referwell.com` | `Nurse@123` | Can view all referrals, claim/triage active items, send mass templates. |
| **GP (Dr. James)** | `gp1@referwell.com` | `Gp1@1234` | Can only view their own patients' referrals, create new referrals. |
| **GP (Dr. Amelia)** | `gp2@referwell.com` | `Gp2@1234` | Can only view their own patients' referrals, create new referrals. |

---

## 🏛️ Architecture Overview

The system is built on **Clean Architecture (Onion Pattern)** to decouple business rules from infrastructure details and external UI frameworks, maximizing testability.

### Project Structure
```
backend/
├── ReferWell.slnx              # .NET 9-style solution file
├── ReferWell.Domain/           # Pure entities, value objects, state rules (No dependencies)
├── ReferWell.Application/      # DTOs, validations, interfaces
├── ReferWell.Infrastructure/   # AppDbContext (SQL Server), SignalR hubs, Background Service
├── ReferWell.Api/              # Presentation Controllers, JWT configuration, Swagger
└── ReferWell.Tests/            # xUnit tests for state machine, priorities, concurrency
frontend/
├── app/                        # Next.js App Router Pages
├── components/                 # Reusable React components (Navbar, SLA countdown timers)
├── context/                    # AuthContext managing session, JWT parsing, and logout
└── tailwind.config.js          # Tailwind CSS style definitions
```

*   **Real-time Synchronization**: SignalR is used to push updates (e.g. when referrals are created, claimed, released, transitioned, or queue weights change) live to all active browser sessions.
*   **Throttled Background Worker**: Mass communications are managed via an in-memory `System.Threading.Channels` queue processed by a .NET `BackgroundService` to ensure a steady, throttled rate of outbound messaging (e.g. 2 messages/sec).

---

## 📝 Architecture Decision Records (ADRs)

### ADR 1: Clean Architecture Pattern
*   **Context**: The application requires highly testable business logic (targeting >= 70% domain coverage) with strict state machine and priority scoring rules.
*   **Decision**: Adopt **Clean Architecture (Onion Architecture)** splitting the codebase into Domain, Application, Infrastructure, and Presentation layers.
*   **Consequences**: The core domain (state machine, prioritization algorithms) has zero dependency on databases, ORMs, or web APIs. This makes unit testing incredibly simple and robust.
*   **Alternatives Rejected**: *Classic N-Tier (3-Layered)*. Tends to breed fat services coupled directly to EF Core DbContext, making unit testing domain rules difficult without complex mock setups.

### ADR 2: MS SQL Server with rowversion Concurrency Tokens
*   **Context**: Multi-user queue safety is critical. Two clinical triage coordinators must not be able to claim or triage the same referral at the same time.
*   **Decision**: Use **MS SQL Server** with Entity Framework Core, utilizing SQL Server's native `rowversion` (a byte array timestamp decorated with `[Timestamp]`).
*   **Consequences**: Every SQL update automatically checks and increments the row version. If two users attempt to update the same referral simultaneously, EF Core detects the mismatch and throws a `DbUpdateConcurrencyException`, allowing us to gracefully notify the user of the claim conflict.
*   **Alternatives Rejected**: *SQLite* (lacks native rowversion column triggers, and does not support full integrated Windows Authentication), *PostgreSQL* (requires configuring custom system column `xmin` in EF Core, which is excellent but the local host only has SQL Server setup).

### ADR 3: Throttled Mass Communications using System.Threading.Channels
*   **Context**: A mass-communication campaign can generate thousands of outbound messages. These must be throttled to avoid hitting rate limits or overwhelming email APIs.
*   **Decision**: Implement a producer-consumer queue using .NET Core's native `System.Threading.Channels` paired with a hosted `BackgroundService` (`MassCommBackgroundService`).
*   **Consequences**: Incoming bulk campaign requests are instantly written to the channel. The background worker pulls jobs and messages sequentially, sleeping for 500ms between each transmission to guarantee a rate of 2 messages/sec.
*   **Alternatives Rejected**: *Hangfire / Quartz.NET*. Adding a heavy scheduling database framework adds complexity, dependencies, and setup friction for a task comfortably resolved by standard .NET asynchronous channels.
