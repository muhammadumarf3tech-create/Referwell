using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Domain.Exceptions;
using ReferWell.Domain.Services;
using ReferWell.Infrastructure.Data;
using ReferWell.Infrastructure.Hubs;
using System.IO;
using System.Security.Claims;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferralsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<QueueHub> _hub;

    public ReferralsController(AppDbContext db, IHubContext<QueueHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);
    
    private List<string> CurrentRoles =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

    [HttpGet("next-case-no")]
    public async Task<IActionResult> GetNextCaseNo()
    {
        var caseNo = await AllocateNextCaseNoAsync();
        return Ok(new { caseNo });
    }

    // ── GET /api/referrals ────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetReferrals(
        [FromQuery] string? status, 
        [FromQuery] string? urgency, 
        [FromQuery] string? assignedTo,
        [FromQuery] string? patientSearch,
        [FromQuery] string? caseNo,
        [FromQuery] string? sortBy,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool? slaBreach,
        [FromQuery] bool? isMigrated,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 15;
        if (pageSize > 100) pageSize = 100;

        // Keep SlaBreach in sync for overdue Received referrals (time-to-first-triage SLA)
        await SyncSlaBreachesAsync();

        var query = _db.Referrals
            .Include(r => r.CreatedByUser)
            .Include(r => r.ClaimedByUser)
            .Include(r => r.AssignedToUser)
            .Include(r => r.Patient)
            .Include(r => r.Attachments)
            .AsQueryable();

        // RBAC: Non-admins only see referrals assigned to them
        if (!CurrentRoles.Contains("Admin"))
        {
            query = query.Where(r => r.AssignedToUserId == CurrentUserId);
        }
        else if (!string.IsNullOrEmpty(assignedTo))
        {
            var assigneeList = assignedTo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => Guid.TryParse(id, out var parsedId) ? (Guid?)parsedId : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (assigneeList.Any())
            {
                query = query.Where(r => r.AssignedToUserId.HasValue && assigneeList.Contains(r.AssignedToUserId.Value));
            }
        }

        if (!string.IsNullOrEmpty(status))
        {
            var statusList = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.TryParse<ReferralStatus>(s, out var parsedStatus) ? (ReferralStatus?)parsedStatus : null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();
            if (statusList.Any())
            {
                query = query.Where(r => statusList.Contains(r.Status));
            }
        }

        if (!string.IsNullOrEmpty(urgency))
        {
            var urgencyList = urgency.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(u => Enum.TryParse<UrgencyLevel>(u, out var parsedUrgency) ? (UrgencyLevel?)parsedUrgency : null)
                .Where(u => u.HasValue)
                .Select(u => u!.Value)
                .ToList();
            if (urgencyList.Any())
            {
                query = query.Where(r => urgencyList.Contains(r.Urgency));
            }
        }

        if (!string.IsNullOrEmpty(patientSearch))
        {
            var cleanSearch = patientSearch.Trim();
            query = query.Where(r => r.Patient != null && (r.Patient.Name.Contains(cleanSearch) || r.Patient.NhiNumber.Contains(cleanSearch)));
        }

        if (!string.IsNullOrEmpty(caseNo))
        {
            var cleanCaseNo = caseNo.Trim();
            query = query.Where(r => r.CaseNo.Contains(cleanCaseNo));
        }

        if (fromDate.HasValue)
        {
            var fromLocal = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Local);
            query = query.Where(r => r.ReceivedAt >= fromLocal);
        }

        if (toDate.HasValue)
        {
            var toLocal = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local);
            query = query.Where(r => r.ReceivedAt <= toLocal);
        }

        if (slaBreach == true)
        {
            // Paused = waiting on patient → not an active breach for filters/stats
            query = query.Where(r => r.SlaBreach && !r.SlaPaused);
        }

        if (isMigrated.HasValue)
        {
            query = query.Where(r => r.IsMigrated == isMigrated.Value);
        }

        // Order before paginating
        if (sortBy == "receivedDate")
        {
            query = query.OrderByDescending(r => r.ReceivedAt);
        }
        else
        {
            query = query.OrderByDescending(r => r.PriorityScore).ThenBy(r => r.SlaDeadline);
        }

        // Count stats *before* paginating but *after* applying filters
        var totalCount = await query.CountAsync();
        var activeCount = await query.CountAsync(r => r.Status != ReferralStatus.Completed && r.Status != ReferralStatus.Declined);
        var urgentCount = await query.CountAsync(r => r.Urgency == UrgencyLevel.Urgent && r.Status != ReferralStatus.Completed);
        var breachedCount = await query.CountAsync(r => r.SlaBreach && !r.SlaPaused);

        var referrals = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.CaseNo,
                r.IsMigrated,
                PatientId = r.PatientId,
                PatientName = r.Patient != null ? r.Patient.Name : string.Empty,
                PatientDateOfBirth = r.Patient != null ? r.Patient.DateOfBirth : DateTime.Now,
                r.SpecialistType,
                r.Reason,
                Urgency = r.Urgency.ToString(),
                Status = r.Status.ToString(),
                r.PriorityScore,
                r.ReceivedAt,
                r.SlaDeadline,
                r.SlaBreach,
                r.SlaPaused,
                r.SlaPausedAt,
                r.SlaPauseReason,
                r.CreatedAt,
                r.RowVersion,
                CreatedByUser = r.CreatedByUser == null ? null : new { r.CreatedByUser.FullName, r.CreatedByUser.Email, r.CreatedByUser.Title },
                ClaimedByUser = r.ClaimedByUser == null ? null : new { r.ClaimedByUser.FullName, r.ClaimedByUser.Email, r.ClaimedByUser.Title },
                AssignedToUser = r.AssignedToUser == null ? null : new { r.AssignedToUser.FullName, r.AssignedToUser.Email, r.AssignedToUser.Title },
                Attachments = r.Attachments.Select(a => new { a.Id, a.FileName, a.FilePath, a.ContentType }).ToList(),
                r.ClaimedAt
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return Ok(new
        {
            items = referrals,
            totalCount,
            activeCount,
            urgentCount,
            breachedCount,
            page,
            pageSize,
            totalPages
        });
    }

    // ── GET /api/referrals/{id} ───────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReferral(Guid id)
    {
        var referral = await _db.Referrals
            .Include(r => r.CreatedByUser)
            .Include(r => r.ClaimedByUser)
            .Include(r => r.AssignedToUser)
            .Include(r => r.Patient)
            .Include(r => r.Attachments)
            .Include(r => r.AuditLogs)
                .ThenInclude(a => a.PerformedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (referral == null) return NotFound();

        // RBAC: Admin/TriageNurse, assignee, creator, or claimer
        if (!CanAccessReferral(referral))
            return Forbid();

        return Ok(referral);
    }

    // ── POST /api/referrals ───────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreateReferral([FromBody] CreateReferralRequest request)
    {
        var weights = await GetWeights();

        var patient = await _db.Patients.FindAsync(request.PatientId);
        if (patient == null) return BadRequest(new { message = "Patient not found." });

        var caseNo = await AllocateNextCaseNoAsync();

        var receivedAt = DateTime.Now;
        var referral = new Referral
        {
            PatientId = request.PatientId,
            SpecialistType = request.SpecialistType,
            Reason = request.Reason,
            Urgency = request.Urgency,
            CreatedByUserId = CurrentUserId,
            ReferringGPId = CurrentUserId.ToString(),
            ReceivedAt = receivedAt,
            SlaDeadline = Referral.CalculateSlaDeadline(request.Urgency, receivedAt),
            AssignedToUserId = request.AssignedToUserId ?? CurrentUserId,
            CaseNo = caseNo
        };

        referral.PriorityScore = PriorityCalculator.Calculate(
            referral.Urgency, referral.ReceivedAt, patient.DateOfBirth,
            weights.urgency, weights.waittime, weights.patient);

        _db.Referrals.Add(referral);

        _db.AuditLogs.Add(new AuditLog
        {
            ReferralId = referral.Id,
            PerformedByUserId = CurrentUserId,
            Action = "Created",
            ToStatus = ReferralStatus.Received
        });

        await _db.SaveChangesAsync();
        await _hub.Clients.Group("QueueGroup").SendAsync("ReferralCreated", referral.Id);
        return CreatedAtAction(nameof(GetReferral), new { id = referral.Id }, referral);
    }

    // ── PUT /api/referrals/{id} ───────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReferral(Guid id, [FromBody] UpdateReferralRequest request)
    {
        var referral = await _db.Referrals
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (referral == null) return NotFound();

        // RBAC check for GPs (pure GP only — TriageNurse and Admin can edit any)
        if (CurrentRoles.Contains("GP") && !CurrentRoles.Contains("Admin") && !CurrentRoles.Contains("TriageNurse") && referral.CreatedByUserId != CurrentUserId)
            return Forbid();

        // Optimistic concurrency: set the OriginalValue so EF detects if another user saved first
        if (request.RowVersion != null)
        {
            _db.Entry(referral).Property(r => r.RowVersion).OriginalValue = request.RowVersion;
        }

        referral.SpecialistType = request.SpecialistType;
        referral.Reason = request.Reason;
        referral.Urgency = request.Urgency;
        referral.AssignedToUserId = request.AssignedToUserId;
        referral.UpdatedAt = DateTime.Now;

        var weights = await GetWeights();
        referral.SlaDeadline = Referral.CalculateSlaDeadline(referral.Urgency, referral.ReceivedAt);
        referral.EvaluateSlaBreach();
        referral.PriorityScore = PriorityCalculator.Calculate(
            referral.Urgency, referral.ReceivedAt, referral.Patient?.DateOfBirth ?? DateTime.Now,
            weights.urgency, weights.waittime, weights.patient);

        _db.AuditLogs.Add(new AuditLog
        {
            ReferralId = referral.Id,
            PerformedByUserId = CurrentUserId,
            Action = "Updated"
        });

        try
        {
            await _db.SaveChangesAsync();
            await _hub.Clients.Group("QueueGroup").SendAsync("ReferralUpdated", referral.Id);
            return Ok(new { message = "Referral updated successfully.", rowVersion = referral.RowVersion });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This referral was recently modified by another user. Please close and reopen the form to get the latest version before saving." });
        }
    }

    // ── POST /api/referrals/{id}/claim ────────────────────────────────────────
    [HttpPost("{id}/claim")]
    [Authorize(Roles = "TriageNurse,Admin,GP")]
    public async Task<IActionResult> ClaimReferral(Guid id, [FromBody] ConcurrencyRequest req)
    {
        var referral = await _db.Referrals.FindAsync(id);
        if (referral == null) return NotFound();

        // Attach rowversion for concurrency check
        _db.Entry(referral).Property(r => r.RowVersion).OriginalValue = req.RowVersion;

        try
        {
            referral.Claim(CurrentUserId);
            _db.AuditLogs.Add(new AuditLog
            {
                ReferralId = referral.Id,
                PerformedByUserId = CurrentUserId,
                Action = "Claimed"
            });
            await _db.SaveChangesAsync();
            await _hub.Clients.Group("QueueGroup").SendAsync("ReferralClaimed", new { id, claimedBy = CurrentUserId });
            return Ok(new { message = "Referral claimed successfully.", rowVersion = referral.RowVersion });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This referral was modified by another user. Please refresh and try again." });
        }
        catch (Domain.Exceptions.ReferralAlreadyClaimedException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ── POST /api/referrals/{id}/release ─────────────────────────────────────
    [HttpPost("{id}/release")]
    [Authorize(Roles = "TriageNurse,Admin,GP")]
    public async Task<IActionResult> ReleaseReferral(Guid id)
    {
        var referral = await _db.Referrals.FindAsync(id);
        if (referral == null) return NotFound();

        // Only the claimer or Admin may release
        if (!CurrentRoles.Contains("Admin")
            && referral.ClaimedByUserId.HasValue
            && referral.ClaimedByUserId != CurrentUserId)
            return Forbid();

        referral.Release();
        _db.AuditLogs.Add(new AuditLog { ReferralId = id, PerformedByUserId = CurrentUserId, Action = "Released" });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group("QueueGroup").SendAsync("ReferralReleased", id);
        return Ok(new { message = "Referral released.", rowVersion = referral.RowVersion });
    }

    // ── POST /api/referrals/{id}/transition ───────────────────────────────────
    [HttpPost("{id}/transition")]
    [Authorize(Roles = "TriageNurse,Admin,GP")]
    public async Task<IActionResult> TransitionReferral(Guid id, [FromBody] TransitionRequest req)
    {
        var referral = await _db.Referrals.FindAsync(id);
        if (referral == null) return NotFound();

        try
        {
            var fromStatus = referral.Status;
            referral.TransitionTo(req.NewStatus);

            _db.AuditLogs.Add(new AuditLog
            {
                ReferralId = id,
                PerformedByUserId = CurrentUserId,
                Action = "StatusChanged",
                FromStatus = fromStatus,
                ToStatus = req.NewStatus,
                Notes = req.Notes
            });

            await _db.SaveChangesAsync();
            await _hub.Clients.Group("QueueGroup").SendAsync("ReferralUpdated", id);
            return Ok(new { message = "Status updated.", rowVersion = referral.RowVersion });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Unable to update status: the referral was modified by another user. Please refresh and try again." });
        }
        catch (InvalidReferralTransitionException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Unable to update status. Please try again." });
        }
    }

    // ── POST /api/referrals/{id}/pause-sla ────────────────────────────────────
    [HttpPost("{id}/pause-sla")]
    [Authorize(Roles = "TriageNurse,Admin,GP")]
    public async Task<IActionResult> PauseSla(Guid id, [FromBody] PauseSlaRequest req)
    {
        var referral = await _db.Referrals.FindAsync(id);
        if (referral == null) return NotFound();

        if (referral.ClaimedByUserId.HasValue && referral.ClaimedByUserId != CurrentUserId)
            return Conflict(new { message = "This referral is claimed by another user. Claim or release it before pausing SLA." });

        _db.Entry(referral).Property(r => r.RowVersion).OriginalValue = req.RowVersion;

        try
        {
            referral.PauseSla(req.Reason ?? "WaitingOnPatient");

            _db.AuditLogs.Add(new AuditLog
            {
                ReferralId = id,
                PerformedByUserId = CurrentUserId,
                Action = "SlaPaused",
                Notes = referral.SlaPauseReason
            });

            await _db.SaveChangesAsync();
            await _hub.Clients.Group("QueueGroup").SendAsync("ReferralUpdated", id);
            return Ok(new
            {
                message = "SLA paused (waiting on patient).",
                rowVersion = referral.RowVersion,
                slaPaused = referral.SlaPaused,
                slaPausedAt = referral.SlaPausedAt,
                slaPauseReason = referral.SlaPauseReason,
                slaDeadline = referral.SlaDeadline
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Unable to pause SLA: the referral was modified by another user. Please refresh and try again." });
        }
        catch (InvalidSlaPauseException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /api/referrals/{id}/resume-sla ───────────────────────────────────
    [HttpPost("{id}/resume-sla")]
    [Authorize(Roles = "TriageNurse,Admin,GP")]
    public async Task<IActionResult> ResumeSla(Guid id, [FromBody] ConcurrencyRequest req)
    {
        var referral = await _db.Referrals.FindAsync(id);
        if (referral == null) return NotFound();

        if (referral.ClaimedByUserId.HasValue && referral.ClaimedByUserId != CurrentUserId)
            return Conflict(new { message = "This referral is claimed by another user. Claim or release it before resuming SLA." });

        _db.Entry(referral).Property(r => r.RowVersion).OriginalValue = req.RowVersion;

        try
        {
            var pausedAt = referral.SlaPausedAt;
            referral.ResumeSla();

            _db.AuditLogs.Add(new AuditLog
            {
                ReferralId = id,
                PerformedByUserId = CurrentUserId,
                Action = "SlaResumed",
                Notes = pausedAt.HasValue
                    ? $"Paused since {pausedAt:dd MMM yyyy HH:mm}; deadline extended to {referral.SlaDeadline:dd MMM yyyy HH:mm}"
                    : null
            });

            await _db.SaveChangesAsync();
            await _hub.Clients.Group("QueueGroup").SendAsync("ReferralUpdated", id);
            return Ok(new
            {
                message = "SLA resumed; deadline extended by the paused duration.",
                rowVersion = referral.RowVersion,
                slaPaused = referral.SlaPaused,
                slaDeadline = referral.SlaDeadline,
                slaBreach = referral.SlaBreach
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Unable to resume SLA: the referral was modified by another user. Please refresh and try again." });
        }
        catch (InvalidSlaPauseException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── ATTACHMENTS ──────────────────────────────────────────────────────────
    private const long MaxPdfBytes = 20L * 1024 * 1024; // 20 MB

    [HttpPost("{id}/attachments")]
    [RequestSizeLimit(MaxPdfBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxPdfBytes)]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file)
    {
        var referral = await _db.Referrals.FindAsync(id);
        if (referral == null) return NotFound();

        if (!CanAccessReferral(referral))
            return Forbid();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (file.Length > MaxPdfBytes)
            return BadRequest(new { message = "PDF attachments must be 20 MB or smaller." });

        // 1. Extension validation (case-insensitive)
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only PDF file attachments (.pdf) are allowed." });
        }

        // 2. MIME type validation (case-insensitive)
        if (string.IsNullOrEmpty(file.ContentType) || !file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Invalid file type. Only application/pdf MIME type is allowed." });
        }

        // 3. Security validation: Magic number / signature check (%PDF- = 0x25 0x50 0x44 0x46)
        try
        {
            using (var stream = file.OpenReadStream())
            {
                var buffer = new byte[4];
                var read = await stream.ReadAsync(buffer, 0, 4);
                if (read < 4 || buffer[0] != 0x25 || buffer[1] != 0x50 || buffer[2] != 0x44 || buffer[3] != 0x46)
                {
                    return BadRequest(new { message = "Security validation failed: File signature does not match PDF format." });
                }
            }
        }
        catch (Exception)
        {
            return BadRequest(new { message = "Failed to perform security validation on the uploaded file." });
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileId = Guid.NewGuid();
        var extension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{fileId}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new ReferralAttachment
        {
            Id = fileId,
            ReferralId = id,
            FileName = file.FileName,
            FilePath = $"/uploads/{uniqueFileName}",
            ContentType = file.ContentType
        };

        _db.ReferralAttachments.Add(attachment);
        _db.AuditLogs.Add(new AuditLog { ReferralId = id, PerformedByUserId = CurrentUserId, Action = $"Uploaded {file.FileName}" });
        await _db.SaveChangesAsync();

        await _hub.Clients.Group("QueueGroup").SendAsync("ReferralUpdated", referral.Id);
        return Ok(attachment);
    }

    [HttpGet("attachments/{attachmentId}")]
    public async Task<IActionResult> GetAttachment(Guid attachmentId, [FromQuery] bool download = false)
    {
        var attachment = await _db.ReferralAttachments
            .Include(a => a.Referral)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);
        if (attachment?.Referral == null) return NotFound();

        if (!CanAccessReferral(attachment.Referral))
            return Forbid();

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        var filePath = Path.Combine(uploadsFolder, Path.GetFileName(attachment.FilePath));

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        _db.AuditLogs.Add(new AuditLog
        {
            ReferralId = attachment.ReferralId,
            PerformedByUserId = CurrentUserId,
            Action = download ? $"Downloaded {attachment.FileName}" : $"Viewed {attachment.FileName}"
        });
        await _db.SaveChangesAsync();

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        if (download)
        {
            return File(fileBytes, attachment.ContentType, attachment.FileName);
        }

        Response.Headers.Append("Content-Disposition", "inline");
        return File(fileBytes, attachment.ContentType);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<string> AllocateNextCaseNoAsync()
    {
        var existing = await _db.Referrals.AsNoTracking()
            .Select(r => r.CaseNo)
            .ToListAsync();
        return CaseNoGenerator.Next(existing);
    }

    private bool CanAccessReferral(Referral referral)
    {
        if (CurrentRoles.Contains("Admin") || CurrentRoles.Contains("TriageNurse"))
            return true;

        return referral.AssignedToUserId == CurrentUserId
            || referral.CreatedByUserId == CurrentUserId
            || referral.ClaimedByUserId == CurrentUserId;
    }

    private async Task SyncSlaBreachesAsync()
    {
        var now = DateTime.Now;
        var overdue = await _db.Referrals
            .Where(r => r.Status == ReferralStatus.Received && !r.SlaBreach && !r.SlaPaused && r.SlaDeadline < now)
            .ToListAsync();

        if (overdue.Count == 0) return;

        foreach (var referral in overdue)
            referral.EvaluateSlaBreach(now);

        await _db.SaveChangesAsync();
    }

    private async Task<(double urgency, double waittime, double patient)> GetWeights()
    {
        var configs = await _db.SystemConfigs.ToListAsync();
        double Get(string key, double def) =>
            double.TryParse(configs.FirstOrDefault(c => c.Key == key)?.Value, out var v) ? v : def;
        return (Get("weight_urgency", 50), Get("weight_waittime", 30), Get("weight_patient", 20));
    }
}

public record CreateReferralRequest(
    Guid PatientId, string SpecialistType, string Reason, UrgencyLevel Urgency, Guid? AssignedToUserId);

public record UpdateReferralRequest(
    string SpecialistType, string Reason, UrgencyLevel Urgency, Guid? AssignedToUserId, byte[]? RowVersion);

public record ConcurrencyRequest(byte[] RowVersion);
public record TransitionRequest(ReferralStatus NewStatus, byte[] RowVersion, string? Notes);
public record PauseSlaRequest(byte[] RowVersion, string? Reason = "WaitingOnPatient");
