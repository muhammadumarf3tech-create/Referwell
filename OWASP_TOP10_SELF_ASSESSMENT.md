# OWASP Top 10 Self-Assessment — ReferWell

**Date:** 12 July 2026  
**Scope:** ReferWell API (`ReferWell.Api`) and Next.js frontend  
**Standard:** [OWASP Top 10:2021](https://owasp.org/Top10/)

This is a short self-assessment of security controls against the OWASP Top 10. It supports the Security Baseline NFR and marks residual risks for remediation.

---

## Summary

| ID | Category | Rating | Status |
|----|----------|--------|--------|
| A01 | Broken Access Control | Mitigated | JWT + roles/menu auth; attachment downloads authenticated; SignalR hub authorised |
| A02 | Cryptographic Failures | Mitigated | BCrypt passwords; JWT key from config/User Secrets (not committed in base `appsettings.json`) |
| A03 | Injection | Mitigated | EF Core LINQ only (parameterised); no raw SQL |
| A04 | Insecure Design | Partial | Domain state machine + priority rules; threat model is lightweight |
| A05 | Security Misconfiguration | Partial | Security headers; Swagger Dev-only; secrets via Development/env; CORS localhost |
| A06 | Vulnerable Components | Partial | .NET 8 / maintained NuGet; CI build; no automated SCA yet |
| A07 | Identification & Authentication Failures | Mitigated | BCrypt; generic login errors; login success/failure audited; JWT expiry configured |
| A08 | Software and Data Integrity Failures | Partial | Optimistic concurrency (`RowVersion`); migrations on startup (ops risk in prod) |
| A09 | Security Logging & Monitoring Failures | Mitigated | Referral `AuditLog` + `SecurityAuditEvent` for auth/admin/config/mass-comm |
| A10 | Server-Side Request Forgery | N/A / Low | No user-controlled outbound URL fetch |

---

## A01 — Broken Access Control

**Controls**
- Controllers use `[Authorize]`; sensitive actions use `[Authorize(Roles=...)]` or `[MenuAuthorize]`.
- PDF attachment GET requires authentication and referral access checks (`CanAccessReferral`).
- `QueueHub` requires `[Authorize]` (token via SignalR `access_token` query for hub path only).
- Referral release restricted to claimer or Admin.
- User directory PII (email/phone) limited to User Management menu holders.

**Residual risk:** TriageNurse/Admin can access any referral by ID (by design for triage queue). Token in SignalR query string can appear in proxy logs.

---

## A02 — Cryptographic Failures

**Controls**
- Passwords stored as BCrypt hashes only (no plaintext password column).
- JWT signing key loaded from configuration; base `appsettings.json` ships with empty key; local key lives in `appsettings.Development.json` (gitignored) or `Jwt__Key` env / User Secrets.
- Example file: `appsettings.Development.json.example`.

**Residual risk:** Seed/demo accounts use known passwords for local demos; change before any shared environment. Prefer HTTPS in production.

---

## A03 — Injection

**Controls**
- All data access via EF Core LINQ (parameterised).
- Mass-comm merge fields allowlisted (template injection limited).
- FluentValidation on write DTOs (length, email, password complexity, enums).

---

## A04 — Insecure Design

**Controls**
- Referral status state machine with enforced transitions.
- Priority scoring and SLA rules in domain layer.
- Optimistic concurrency on referral updates.

**Residual risk:** No formal threat-model artefact beyond this assessment and SRS NFRs.

---

## A05 — Security Misconfiguration

**Controls**
- Response headers: `X-Frame-Options`, `X-Content-Type-Options`, `X-XSS-Protection`, `Referrer-Policy`.
- Swagger enabled only in Development.
- PDF upload: extension, MIME, magic bytes, **max 20 MB**.
- CORS restricted to frontend origin.

**Residual risk:** `AllowedHosts: *`; no HSTS middleware yet (add when HTTPS is enforced in production).

---

## A06 — Vulnerable and Outdated Components

**Controls**
- Target framework .NET 8; packages restored via NuGet in CI.

**Residual risk:** Add Dependabot / `dotnet list package --vulnerable` to CI as a follow-up.

---

## A07 — Identification and Authentication Failures

**Controls**
- JWT Bearer authentication; role claims issued at login.
- Generic “Invalid credentials” message (no user enumeration).
- Login success and failure written to `SecurityAuditEvent`.
- Quick-login buttons only rendered in frontend Development builds.

**Residual risk:** No account lockout / rate limiting on `/api/auth/login` yet; recommend adding ASP.NET rate limiting for production.

---

## A08 — Software and Data Integrity Failures

**Controls**
- SQL Server `rowversion` concurrency token on referrals.
- DTOs used for writes (entities not mass-bound from JSON).

**Residual risk:** Auto-migrate on API startup is convenient for demos; production should run migrations as a controlled release step.

---

## A09 — Security Logging and Monitoring Failures

**Controls**
- Clinical/referral `AuditLog`: create, update, claim, release, status, SLA pause/resume, upload/view/download attachment, import.
- `SecurityAuditEvent`: login success/failure, user admin, priority weights, menu access, mass-comm campaign create (with IP where available).

**Residual risk:** No SIEM/alerting integration; logs are DB-backed for review.

---

## A10 — Server-Side Request Forgery (SSRF)

**Assessment:** No endpoints accept arbitrary URLs for server-side fetch. Risk considered low / not applicable.

---

## Evidence map (key files)

| Control | Location |
|---------|----------|
| JWT + pipeline | `backend/ReferWell.Api/Program.cs` |
| Auth login + audit | `AuthController.cs` |
| RBAC / menu | `Authorization/MenuAuthorizeAttribute.cs` |
| Attachment auth + 20 MB | `ReferralsController.cs` |
| FluentValidation | `Validation/RequestValidators.cs` |
| Security audit | `SecurityAuditEvent.cs`, `SecurityAuditService.cs` |
| SignalR auth | `QueueHub.cs` |
| Secrets hygiene | `appsettings.json`, `.gitignore`, `appsettings.Development.json.example` |

---

## Conclusion

ReferWell meets a **practical OWASP Top 10 baseline** for a triage/referral application: authentication and authorisation are enforced server-side, data access is parameterised, secrets are not stored in the committed base config, inputs are validated, and audit trails cover clinical and security-sensitive actions. Remaining gaps (login rate limiting, HTTPS/HSTS in production, dependency scanning) are documented above as residual risks.
