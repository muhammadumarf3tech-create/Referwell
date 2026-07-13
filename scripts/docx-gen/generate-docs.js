/**
 * Generates ReferWell Requirements Specification and User Manual (.docx)
 */
const {
  Document, Packer, Paragraph, TextRun, HeadingLevel, Table, TableRow, TableCell,
  WidthType, BorderStyle, AlignmentType, PageNumber, Header, Footer, LevelFormat,
} = require('docx');
const fs = require('fs');
const path = require('path');

const OUT_DIR = path.resolve(__dirname, '..', '..', 'docs');
fs.mkdirSync(OUT_DIR, { recursive: true });

const thin = { style: BorderStyle.SINGLE, size: 4, color: 'CBD5E1' };
const borders = { top: thin, bottom: thin, left: thin, right: thin };
const headerFill = '1E3A5F';
const accentFill = 'EFF6FF';

function h1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    spacing: { before: 360, after: 160 },
    children: [new TextRun({ text, bold: true, color: '1E3A5F' })],
  });
}
function h2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    spacing: { before: 280, after: 120 },
    children: [new TextRun({ text, bold: true, color: '1E40AF' })],
  });
}
function h3(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    spacing: { before: 200, after: 80 },
    children: [new TextRun({ text, bold: true, color: '334155' })],
  });
}
function p(text, opts = {}) {
  return new Paragraph({
    spacing: { after: 120 },
    ...opts,
    children: [new TextRun({ text, size: 22, ...opts.run })],
  });
}
function boldP(label, text) {
  return new Paragraph({
    spacing: { after: 100 },
    children: [
      new TextRun({ text: label, bold: true, size: 22 }),
      new TextRun({ text, size: 22 }),
    ],
  });
}
function bullet(text, ref = 'bullets') {
  return new Paragraph({
    numbering: { reference: ref, level: 0 },
    spacing: { after: 60 },
    children: [new TextRun({ text, size: 22 })],
  });
}
function numbered(text, ref = 'numbers') {
  return new Paragraph({
    numbering: { reference: ref, level: 0 },
    spacing: { after: 60 },
    children: [new TextRun({ text, size: 22 })],
  });
}
function note(text) {
  return new Paragraph({
    spacing: { before: 80, after: 120 },
    children: [
      new TextRun({ text: 'Note: ', bold: true, italics: true, size: 20, color: '64748B' }),
      new TextRun({ text, italics: true, size: 20, color: '64748B' }),
    ],
  });
}
function cell(text, opts = {}) {
  const { bold = false, fill = 'FFFFFF', width = 2340, center = false, color = '0F172A' } = opts;
  return new TableCell({
    borders,
    width: { size: width, type: WidthType.DXA },
    shading: { fill },
    children: [
      new Paragraph({
        alignment: center ? AlignmentType.CENTER : AlignmentType.LEFT,
        children: [
          new TextRun({
            text,
            bold,
            size: 18,
            color: fill === headerFill ? 'FFFFFF' : color,
          }),
        ],
      }),
    ],
  });
}
function simpleTable(headers, rows, colWidths) {
  const widths = colWidths || headers.map(() => Math.floor(9360 / headers.length));
  return new Table({
    width: { size: 9360, type: WidthType.DXA },
    columnWidths: widths,
    rows: [
      new TableRow({
        children: headers.map((h, i) =>
          cell(h, { bold: true, fill: headerFill, width: widths[i], center: true })
        ),
      }),
      ...rows.map((row, ri) =>
        new TableRow({
          children: row.map((c, i) =>
            cell(String(c), {
              width: widths[i],
              fill: ri % 2 === 0 ? accentFill : 'FFFFFF',
            })
          ),
        })
      ),
    ],
  });
}
function titlePage(title, subtitle, metaLines) {
  return [
    new Paragraph({ spacing: { before: 1200 }, children: [] }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { after: 200 },
      children: [new TextRun({ text: 'ReferWell', bold: true, size: 56, color: '1E3A5F' })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { after: 400 },
      children: [new TextRun({ text: 'Referral Triage & SLA Queue Management', size: 24, color: '64748B' })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { after: 200 },
      border: { bottom: { style: BorderStyle.SINGLE, size: 12, color: '2563EB', space: 8 } },
      children: [new TextRun({ text: title, bold: true, size: 40, color: '1E40AF' })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { after: 600 },
      children: [new TextRun({ text: subtitle, size: 22, color: '475569' })],
    }),
    ...metaLines.map((line) =>
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 80 },
        children: [new TextRun({ text: line, size: 20, color: '64748B' })],
      })
    ),
    new Paragraph({ children: [] }),
  ];
}
function pageHeader(docTitle) {
  return new Header({
    children: [
      new Paragraph({
        border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: 'CBD5E1', space: 4 } },
        children: [
          new TextRun({ text: 'ReferWell  |  ', size: 16, color: '64748B' }),
          new TextRun({ text: docTitle, size: 16, bold: true, color: '1E3A5F' }),
        ],
      }),
    ],
  });
}
function pageFooter() {
  return new Footer({
    children: [
      new Paragraph({
        alignment: AlignmentType.CENTER,
        children: [
          new TextRun({ text: 'Page ', size: 16, color: '94A3B8' }),
          new TextRun({ children: [PageNumber.CURRENT], size: 16, color: '94A3B8' }),
          new TextRun({ text: ' of ', size: 16, color: '94A3B8' }),
          new TextRun({ children: [PageNumber.TOTAL_PAGES], size: 16, color: '94A3B8' }),
        ],
      }),
    ],
  });
}
function numberingConfig() {
  return {
    config: [
      {
        reference: 'bullets',
        levels: [{
          level: 0, format: LevelFormat.BULLET, text: '•', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } },
        }],
      },
      {
        reference: 'numbers',
        levels: [{
          level: 0, format: LevelFormat.DECIMAL, text: '%1.', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } },
        }],
      },
      {
        reference: 'steps',
        levels: [{
          level: 0, format: LevelFormat.DECIMAL, text: '%1.', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } },
        }],
      },
      {
        reference: 'fr-auth',
        levels: [{ level: 0, format: LevelFormat.DECIMAL, text: '%1.', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }],
      },
    ],
  };
}

// ─────────────────────────────────────────────────────────────────────────────
// REQUIREMENTS DOCUMENT
// ─────────────────────────────────────────────────────────────────────────────
function buildRequirementsDoc() {
  return new Document({
    numbering: numberingConfig(),
    styles: {
      default: { document: { styles: [{ id: 'Normal', run: { font: 'Calibri', size: 22 } }] } },
    },
    sections: [{
      properties: {
        page: { margin: { top: 720, bottom: 720, left: 720, right: 720 } },
      },
      headers: { default: pageHeader('Software Requirements Specification') },
      footers: { default: pageFooter() },
      children: [
        ...titlePage(
          'Software Requirements Specification',
          'Functional and Non-Functional Requirements',
          [
            'Document Version: 1.1',
            'Date: 13 July 2026',
            'Status: Approved for Implementation',
            'Classification: Internal',
          ]
        ),

        h1('1. Introduction'),
        h2('1.1 Purpose'),
        p('This Software Requirements Specification (SRS) defines the functional and non-functional requirements for ReferWell — a clinical referral triage and Service Level Agreement (SLA) queue management system. The system orchestrates specialist care referrals, enforces contractual SLA deadlines, protects multi-user queue safety with optimistic concurrency, and supports throttled bulk patient messaging.'),

        h2('1.2 Scope'),
        p('ReferWell includes:'),
        bullet('A .NET 8.0 Web API backend following Clean Architecture (Domain, Application, Infrastructure, Api).'),
        bullet('A Next.js App Router (TypeScript) frontend with Tailwind CSS and a shared top navigation layout.'),
        bullet('Entity Framework Core persistence on Microsoft SQL Server with Windows Authentication support.'),
        bullet('Role-Based Access Control (RBAC) with dynamic role–menu mapping for Admin, Triage Nurse, and General Practitioner (GP).'),
        bullet('Real-time queue synchronization via SignalR.'),
        bullet('Throttled mass-communication processing via in-memory channels and a background worker.'),
        bullet('Bulk referral import from CSV for historical/migration data.'),
        bullet('SLA pause/resume controls and automated breach detection.'),

        h2('1.3 Definitions and Abbreviations'),
        simpleTable(
          ['Term', 'Definition'],
          [
            ['GP', 'General Practitioner — creates and owns patient referrals'],
            ['SLA', 'Service Level Agreement — contractual triage deadline'],
            ['RBAC', 'Role-Based Access Control'],
            ['JWT', 'JSON Web Token used for API session authorization'],
            ['Concurrency Token', 'SQL Server rowversion field preventing overwrite races'],
            ['SignalR', 'Real-time push channel for live queue updates'],
            ['Case No', 'Unique human-readable referral case identifier'],
          ],
          [2400, 6960]
        ),

        h1('2. Overall Description'),
        h2('2.1 Product Perspective'),
        p('ReferWell replaces fragmented legacy spreadsheets and databases with a web-based client–server system. Persistent SignalR connections keep the referral queue synchronized across all open browser sessions in real time.'),

        h2('2.2 Product Functions'),
        bullet('Secure login and multi-role session resolution'),
        bullet('New referral entry (GP) with optional PDF attachments'),
        bullet('Active referral queue ordered by dynamic priority score'),
        bullet('Referral claim / release lockout for triage exclusivity'),
        bullet('Referral lifecycle state transitions with optional notes'),
        bullet('Live SLA countdown timers with pause / resume'),
        bullet('Dashboard filtering (urgency, status, specialty, assignee, SLA breach, date range, case number)'),
        bullet('Dynamic priority weight configuration'),
        bullet('Templated mass communications with recipient preview and delivery logs'),
        bullet('User profile management and menu-access administration'),
        bullet('Bulk CSV referral import with validation feedback'),
        bullet('Append-only referral audit timeline'),

        h2('2.3 User Classes'),
        simpleTable(
          ['Role', 'Primary Responsibilities'],
          [
            ['Admin', 'Manage users and menu access; view all referrals; configure priority weights; run mass communications and imports'],
            ['Triage Nurse', 'Claim and triage referrals; transition status; pause/resume SLA; send mass communication campaigns'],
            ['GP', 'Create referrals for own patients; view own referrals; limited queue actions based on menu access'],
          ],
          [2400, 6960]
        ),

        h2('2.4 Operating Environment'),
        bullet('Client: Modern browsers (Chrome, Edge, Firefox, Safari)'),
        bullet('Frontend: Next.js on port 4000 (local default)'),
        bullet('Backend: ASP.NET Core Kestrel API on port 5165 (local default)'),
        bullet('Database: Microsoft SQL Server (LocalDB or SQL Express)'),

        h2('2.5 Design Constraints'),
        bullet('Must compile under .NET 8.0 SDK'),
        bullet('Frontend must use Next.js App Router and Tailwind CSS'),
        bullet('Database must be Microsoft SQL Server (SQLite not permitted)'),
        bullet('JWT signing secrets must not be committed to source control'),

        h1('3. Functional Requirements'),

        h2('3.1 Authentication & Session Management'),
        bullet('FR-1.1: The system shall verify credentials via a secure Login page.'),
        bullet('FR-1.2: Passwords shall be stored and verified using BCrypt hashing (no plain-text password storage).'),
        bullet('FR-1.3: On success, the API shall return a JWT containing user ID, email, name, and role claims.'),
        bullet('FR-1.4: Clients shall send the JWT as an Authorization Bearer token on all protected API calls and SignalR hub connections.'),
        bullet('FR-1.5: Sign-out shall purge the JWT from client local storage.'),

        h2('3.2 Referral Submission & State Machine'),
        bullet('FR-2.1: GPs shall submit referrals with patient details, specialist type, reason, urgency, and optional PDF attachments (≤ 20 MB).'),
        bullet('FR-2.2: Each referral shall receive a unique Case Number.'),
        bullet('FR-2.3: Allowed statuses: Received → Triaged → Accepted/Declined → Booked → Completed.'),
        bullet('FR-2.4: Transitions shall enforce: Received→Triaged; Triaged→Accepted|Declined; Accepted→Booked; Booked→Completed.'),
        bullet('FR-2.5: Optional transition notes shall be recorded in the audit trail.'),
        bullet('FR-2.6: SLA deadlines shall be calculated on receipt: Urgent = 24 hours; Semi-Urgent = 7 days; Routine = 30 days.'),

        h2('3.3 Dynamic Prioritization'),
        bullet('FR-3.1: Priority Score = (W_urgency × U) + (W_waittime × T) + (W_patient × P).'),
        bullet('FR-3.2: Admins shall adjust weight percentages via configuration UI.'),
        bullet('FR-3.3: Weights must sum to exactly 100% before save.'),
        bullet('FR-3.4: Saving weights shall recalculate all active scores and re-sort the queue, broadcasting via SignalR.'),

        h2('3.4 Multi-User Concurrency Claims'),
        bullet('FR-4.1: A triage nurse shall claim a referral to gain exclusive transition rights.'),
        bullet('FR-4.2: Concurrent claims/updates of the same referral shall be rejected.'),
        bullet('FR-4.3: SQL Server rowversion shall detect stale updates; API shall return HTTP 409 Conflict.'),
        bullet('FR-4.4: Client shall show a clear conflict message and prompt refresh.'),

        h2('3.5 Real-Time Queue & Live Sync'),
        bullet('FR-5.1: Dashboard shall connect to the SignalR Queue Hub on load.'),
        bullet('FR-5.2: Hub shall broadcast create, claim, release, transition, SLA pause/resume, and weight-recalculation events.'),
        bullet('FR-5.3: Connected clients shall silently refresh the queue on broadcast.'),

        h2('3.6 SLA Timer Controls'),
        bullet('FR-5A.1: Client shall display a live SLA countdown (updates every 1 second) with warning/overdue colour states.'),
        bullet('FR-5A.2: Authorized users shall pause the SLA clock when waiting on patient information.'),
        bullet('FR-5A.3: Resuming shall extend the deadline by the exact paused duration.'),
        bullet('FR-5A.4: A background service shall scan for breached Received referrals approximately every minute and push alerts.'),
        bullet('FR-5A.5: Completing/closing a referral shall clear any active pause state.'),

        h2('3.7 Mass Communications'),
        bullet('FR-6.1: Nurses/Admins shall create campaigns targeting patients or referring GPs.'),
        bullet('FR-6.2: Recipient filters shall match dashboard filters (status, urgency, specialty, assignee, SLA, dates, case number).'),
        bullet('FR-6.3: Templates shall support merge fields: {PatientName}, {CaseNo}, {SpecialistType}, {Status}, {Urgency}, {SlaDeadline}, {ReceivedDate}, {ReferringGPName}.'),
        bullet('FR-6.4: System shall preview up to 100 resolved recipients before send confirmation.'),
        bullet('FR-6.5: Messages shall enqueue to an in-memory channel processed at ~2 messages/second.'),
        bullet('FR-6.6: Delivery results (Sent/Failed) shall be audited and viewable per campaign.'),

        h2('3.8 User Administration'),
        bullet('FR-7.1: Admins shall create, edit, and deactivate users and assign one or more roles.'),
        bullet('FR-7.2: Admins shall configure which menu items each role can access (Menu Access).'),
        bullet('FR-7.3: API endpoints and frontend navigation shall enforce menu-based authorization.'),

        h2('3.9 Referral Import'),
        bullet('FR-8.1: Authorized users shall upload CSV files to import historical or migrated referrals.'),
        bullet('FR-8.2: Import shall validate rows and report per-row errors without silently discarding invalid data clarity.'),
        bullet('FR-8.3: Successfully imported referrals shall be marked as migrated where applicable and receive Case Numbers.'),

        h2('3.10 Audit Trail'),
        bullet('FR-9.1: Referral detail view shall show an append-only audit timeline of claims, releases, status changes, and SLA actions.'),
        bullet('FR-9.2: Security-sensitive events (e.g. auth failures) may be recorded in a security audit store.'),

        h1('4. Non-Functional Requirements'),

        h2('4.1 Security (OWASP Baseline)'),
        bullet('NFR-1.1: All DB access via EF Core parameterized queries (no raw concatenated SQL).'),
        bullet('NFR-1.2: APIs protected with [Authorize] / [MenuAuthorize].'),
        bullet('NFR-1.3: UI shall hide navigation items the user is not permitted to open.'),
        bullet('NFR-1.4: Security headers: X-Frame-Options DENY; X-Content-Type-Options nosniff; X-XSS-Protection.'),
        bullet('NFR-1.5: Secrets (JWT key, connection strings) via User Secrets, environment variables, or gitignored Development settings.'),
        bullet('NFR-1.6: FluentValidation on write DTOs; PDF type/signature and size checks (≤ 20 MB).'),
        bullet('NFR-1.7: Maintain OWASP Top 10 self-assessment documentation.'),

        h2('4.2 Usability'),
        bullet('NFR-2.1: Professional clinical UI with consistent top navigation across roles.'),
        bullet('NFR-2.2: Dashboard filters and tables shall support searchable multi-select controls for large lists.'),

        h2('4.3 Performance & Reliability'),
        bullet('NFR-3.1: SLA timers tick client-side every 1000 ms.'),
        bullet('NFR-3.2: Mass-comm and SLA breach workers shall not block API request threads.'),
        bullet('NFR-3.3: Domain unit tests shall cover priority scoring, state transitions, claims, and SLA pause/resume logic.'),

        h1('5. Architecture Summary'),
        p('ReferWell uses Clean Architecture:'),
        simpleTable(
          ['Layer', 'Responsibility'],
          [
            ['Domain', 'Entities, enums, priority calculator, case-number rules, SLA clock logic'],
            ['Application', 'DTOs and application contracts'],
            ['Infrastructure', 'EF Core, SQL Server, SignalR hubs, background services'],
            ['Api', 'Controllers, JWT, validation, Swagger'],
            ['Tests', 'xUnit coverage of domain rules'],
          ],
          [2400, 6960]
        ),

        h1('6. Traceability — Screens to Requirements'),
        simpleTable(
          ['Screen / Module', 'Key Requirement IDs'],
          [
            ['Login', 'FR-1.1 – FR-1.5'],
            ['Dashboard / Queue', 'FR-2.*, FR-4.*, FR-5.*, FR-5A.*, FR-9.*'],
            ['New Referral', 'FR-2.1, FR-2.2, FR-2.6'],
            ['Priority Config', 'FR-3.1 – FR-3.4'],
            ['Mass Communications', 'FR-6.1 – FR-6.6'],
            ['User Management', 'FR-7.1'],
            ['Menu Access', 'FR-7.2, FR-7.3'],
            ['Referral Import', 'FR-8.1 – FR-8.3'],
          ],
          [3600, 5760]
        ),

        h1('7. Document Control'),
        boldP('Author: ', 'ReferWell Project Team'),
        boldP('Source baseline: ', 'SRS.md, RELEASE_NOTES.md (v1.1.0), implemented application behaviour'),
        boldP('Related documents: ', 'User Manual; OWASP_TOP10_SELF_ASSESSMENT.md; README.md'),
        note('Where this document and older SRS wording differ (for example urgency levels), the implemented system behaviour documented here takes precedence.'),
      ],
    }],
  });
}

// ─────────────────────────────────────────────────────────────────────────────
// USER MANUAL
// ─────────────────────────────────────────────────────────────────────────────
function buildUserManual() {
  return new Document({
    numbering: numberingConfig(),
    sections: [{
      properties: {
        page: { margin: { top: 720, bottom: 720, left: 720, right: 720 } },
      },
      headers: { default: pageHeader('User Manual') },
      footers: { default: pageFooter() },
      children: [
        ...titlePage(
          'User Manual',
          'How to use ReferWell day-to-day',
          [
            'Document Version: 1.1',
            'Date: 13 July 2026',
            'Audience: Admin, Triage Nurse, General Practitioner',
            'Application: ReferWell Web (browser)',
          ]
        ),

        h1('1. Getting Started'),
        h2('1.1 What is ReferWell?'),
        p('ReferWell helps clinics manage specialist referrals against contractual SLA deadlines. GPs submit referrals; triage nurses claim and progress them through a controlled workflow; admins manage users, permissions, and priority scoring.'),

        h2('1.2 Accessing the Application'),
        numbered('Open a supported browser (Chrome, Edge, Firefox, or Safari).', 'steps'),
        numbered('Go to the ReferWell web address provided by your organisation (local default: http://localhost:4000).', 'steps'),
        numbered('Sign in with the email and password issued by your administrator.', 'steps'),

        h2('1.3 Demo / Test Accounts (Development)'),
        simpleTable(
          ['Role', 'Email', 'Password'],
          [
            ['Admin', 'admin@referwell.com', 'Admin@123'],
            ['Triage Nurse', 'nurse@referwell.com', 'Nurse@123'],
            ['GP (Dr. James)', 'gp1@referwell.com', 'Gp1@1234'],
            ['GP (Dr. Amelia)', 'gp2@referwell.com', 'Gp2@1234'],
          ],
          [2200, 4200, 2960]
        ),
        note('Change or disable demo passwords in any shared or production environment.'),

        h2('1.4 Navigation Overview'),
        p('After login, the top bar shows only the menus your roles are allowed to access:'),
        simpleTable(
          ['Menu', 'Typical Use'],
          [
            ['Dashboard', 'View and work the referral queue'],
            ['Priority Config', 'Adjust how urgency, wait time, and patient factors affect ranking'],
            ['Mass Communications', 'Send filtered bulk messages to patients or GPs'],
            ['User Management', 'Create and maintain user accounts'],
            ['Menu Access', 'Grant or revoke menus per role'],
            ['Referral Import', 'Upload CSV referral batches'],
          ],
          [2800, 6560]
        ),
        p('Use the profile menu (top-right) to confirm your roles and to Sign out.'),

        h1('2. Signing In and Out'),
        h3('Sign in'),
        numbered('Open the Login page.', 'steps'),
        numbered('Enter your email and password.', 'steps'),
        numbered('Click Sign in. You are redirected to the Dashboard (or first permitted page).', 'steps'),
        h3('Sign out'),
        numbered('Open the profile menu in the top-right corner.', 'steps'),
        numbered('Choose Sign out. Your session token is cleared from the browser.', 'steps'),

        h1('3. Dashboard — Referral Queue'),
        p('The Dashboard is the main work surface. Referrals appear in priority order. Live SLA timers update every second. When another user claims or updates a referral, your queue refreshes automatically.'),

        h2('3.1 Understanding a Queue Row'),
        bullet('Case Number — unique referral identifier'),
        bullet('Patient and referring GP details'),
        bullet('Specialist type and urgency (Routine, Semi-Urgent, Urgent)'),
        bullet('Status (Received, Triaged, Accepted, Declined, Booked, Completed)'),
        bullet('Priority score'),
        bullet('SLA countdown (or paused / breached state)'),
        bullet('Claim / assignee information'),

        h2('3.2 Filtering the Queue'),
        p('Use the filter bar to narrow the list:'),
        bullet('Urgency, Status, Specialist type, Assignee'),
        bullet('SLA breached only'),
        bullet('Submission date From / To'),
        bullet('Case number search'),
        note('Multi-select filters support typing to search long lists.'),

        h2('3.3 Opening Referral Details'),
        numbered('Click a referral row (or its details action).', 'steps'),
        numbered('Review clinical details, attachments, and the audit timeline (claims, status changes, SLA pause/resume, notes).', 'steps'),
        numbered('Preview PDF attachments inline; use Download when you need a saved copy.', 'steps'),

        h1('4. Creating a Referral (GP)'),
        numbered('From navigation or Dashboard, open New Referral (when available to your role).', 'steps'),
        numbered('Select or enter the patient.', 'steps'),
        numbered('Choose specialist type, urgency, and enter the reason for referral.', 'steps'),
        numbered('Optionally attach a PDF clinical document (maximum 20 MB).', 'steps'),
        numbered('Submit. The system assigns a Case Number and SLA deadline based on urgency.', 'steps'),
        p('SLA defaults:'),
        simpleTable(
          ['Urgency', 'SLA Deadline'],
          [
            ['Urgent', '24 hours from receipt'],
            ['Semi-Urgent', '7 days from receipt'],
            ['Routine', '30 days from receipt'],
          ],
          [3600, 5760]
        ),
        note('GPs normally see only referrals they created, unless menu/role configuration grants a wider view.'),

        h1('5. Triage Workflow (Triage Nurse / Admin)'),
        h2('5.1 Claiming a Referral'),
        numbered('Locate an unclaimed referral in Received (or applicable) status.', 'steps'),
        numbered('Click Claim. You become the exclusive editor for transitions.', 'steps'),
        numbered('If another user claimed it first, you will see a conflict message — refresh and choose another item.', 'steps'),
        h3('Release'),
        p('If you cannot continue, Release the claim so a colleague can pick it up.'),

        h2('5.2 Status Transitions'),
        p('Progress the referral only along allowed paths:'),
        simpleTable(
          ['From', 'Allowed next status'],
          [
            ['Received', 'Triaged'],
            ['Triaged', 'Accepted or Declined'],
            ['Accepted', 'Booked'],
            ['Booked', 'Completed'],
          ],
          [3600, 5760]
        ),
        numbered('Open the referral and choose the next status action.', 'steps'),
        numbered('Enter an optional note when prompted (stored on the audit timeline).', 'steps'),
        numbered('Confirm. Connected users see the update immediately.', 'steps'),

        h2('5.3 Pause / Resume SLA'),
        p('Use when waiting on the patient (for example missing information) so breach time does not accumulate unfairly.'),
        numbered('Open the referral and choose Pause SLA.', 'steps'),
        numbered('The timer shows a paused state.', 'steps'),
        numbered('When ready, choose Resume SLA. The deadline extends by the exact paused duration.', 'steps'),
        note('Completed/closed referrals clear any pause automatically.'),

        h1('6. Priority Configuration (Admin)'),
        numbered('Open Priority Config from the top menu.', 'steps'),
        numbered('Adjust the sliders for Urgency, Wait Time, and Patient Age/factor weights.', 'steps'),
        numbered('Ensure the three weights total exactly 100%.', 'steps'),
        numbered('Save. All active scores recalculate and the queue re-sorts for everyone connected.', 'steps'),

        h1('7. Mass Communications'),
        p('Send templated messages to patients or referring GPs using the same filters as the Dashboard.'),
        numbered('Open Mass Communications.', 'steps'),
        numbered('Choose recipient type (Patient or Referring GP).', 'steps'),
        numbered('Apply filters to define the audience.', 'steps'),
        numbered('Write Subject and Body using only supported merge fields.', 'steps'),
        numbered('Preview recipients (up to the first 100) and check interpolated values.', 'steps'),
        numbered('Confirm send. Messages are throttled (~2 per second) in the background.', 'steps'),
        numbered('Open a completed campaign to view delivery logs (Sent / Failed).', 'steps'),
        h3('Supported merge fields'),
        p('{PatientName}, {CaseNo}, {SpecialistType}, {Status}, {Urgency}, {SlaDeadline}, {ReceivedDate}, {ReferringGPName}'),

        h1('8. User Management (Admin)'),
        numbered('Open User Management.', 'steps'),
        numbered('Create a user with name, email, password, and one or more roles.', 'steps'),
        numbered('Edit details or deactivate accounts that should no longer sign in.', 'steps'),
        note('Passwords are stored hashed. You never see an existing password after save.'),

        h1('9. Menu Access (Admin)'),
        numbered('Open Menu Access.', 'steps'),
        numbered('For each role (Admin, Triage Nurse, GP), tick the menus that role may see and call.', 'steps'),
        numbered('Save. Navigation and API access update according to the new map.', 'steps'),
        note('Users only see menus granted here. Removing a menu hides it and blocks the related API routes.'),

        h1('10. Referral Import'),
        numbered('Open Referral Import.', 'steps'),
        numbered('Download a sample CSV from the page (or use the samples under public/samples/referral-import) to match the expected columns.', 'steps'),
        numbered('Upload your CSV file.', 'steps'),
        numbered('Review validation results. Fix and re-upload rows that failed.', 'steps'),
        numbered('Confirm successful imports appear on the Dashboard with Case Numbers.', 'steps'),

        h1('11. Tips and Troubleshooting'),
        simpleTable(
          ['Issue', 'What to try'],
          [
            ['Cannot see a menu', 'Ask Admin to grant Menu Access for your role'],
            ['409 Conflict / claim failed', 'Another user updated the referral — refresh and retry'],
            ['Login fails', 'Check email/password; ask Admin to confirm the account is active'],
            ['Queue not updating live', 'Refresh the page; confirm network allows SignalR to the API'],
            ['Attachment rejected', 'Use PDF only, 20 MB or smaller'],
            ['Mass-comm template error', 'Remove unsupported merge fields; use only the listed tokens'],
            ['Weights will not save', 'Ensure Urgency + Wait Time + Patient weights equal 100%'],
          ],
          [3200, 6160]
        ),

        h1('12. Roles at a Glance'),
        simpleTable(
          ['Capability', 'Admin', 'Triage Nurse', 'GP'],
          [
            ['View all referrals', 'Yes*', 'Yes*', 'Own*'],
            ['Create referral', 'If menu granted', 'If menu granted', 'Yes*'],
            ['Claim / triage', 'Yes*', 'Yes*', 'Limited*'],
            ['Priority config', 'Yes*', 'No*', 'No*'],
            ['Mass communications', 'Yes*', 'Yes*', 'No*'],
            ['User / menu admin', 'Yes*', 'No*', 'No*'],
            ['Referral import', 'Yes*', 'If menu granted', 'If menu granted'],
          ],
          [2800, 2200, 2200, 2160]
        ),
        note('* Exact access depends on Menu Access configuration. Defaults match the seeded demo data.'),

        h1('13. Support'),
        p('For access problems, contact your ReferWell administrator. For technical hosting issues, contact the system operator who maintains the API, database, and JWT configuration described in the project README.'),
        boldP('Related document: ', 'Software Requirements Specification (separate Word document).'),
      ],
    }],
  });
}

async function main() {
  const reqPath = path.join(OUT_DIR, 'ReferWell_Requirements_Specification.docx');
  const manPath = path.join(OUT_DIR, 'ReferWell_User_Manual.docx');

  const [reqBuf, manBuf] = await Promise.all([
    Packer.toBuffer(buildRequirementsDoc()),
    Packer.toBuffer(buildUserManual()),
  ]);

  fs.writeFileSync(reqPath, reqBuf);
  fs.writeFileSync(manPath, manBuf);

  console.log('Created:');
  console.log(' ', reqPath);
  console.log(' ', manPath);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
