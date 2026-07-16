using Microsoft.EntityFrameworkCore;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Application.Common.Models;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Domain.Exceptions;
using ReferWell.Domain.Services;

namespace ReferWell.Application.Referrals;

public class ReferralService : IReferralService
{
    private const long MaxPdfBytes = 20L * 1024 * 1024; // 20 MB

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IQueueNotifier _queueNotifier;
    private readonly IAttachmentStorage _attachments;

    public ReferralService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IQueueNotifier queueNotifier,
        IAttachmentStorage attachments)
    {
        _db = db;
        _currentUser = currentUser;
        _queueNotifier = queueNotifier;
        _attachments = attachments;
    }

    public async Task<AppResult> GetNextCaseNoAsync(CancellationToken ct = default)
    {
        var caseNo = await AllocateNextCaseNoAsync(ct);
        return AppResult.Success(new { caseNo });
    }

    public async Task<AppResult> GetReferralsAsync(GetReferralsQuery q, CancellationToken ct = default)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize < 1 ? 15 : q.PageSize;
        if (pageSize > 100) pageSize = 100;

        await SyncSlaBreachesAsync(ct);

        var query = _db.Referrals
            .Include(r => r.CreatedByUser)
            .Include(r => r.ClaimedByUser)
            .Include(r => r.AssignedToUser)
            .Include(r => r.Patient)
            .Include(r => r.Attachments)
            .AsQueryable();

        var isAdmin = _currentUser.IsInRole("Admin");
        var isTriageNurse = _currentUser.IsInRole("TriageNurse");
        var isGpOnly = _currentUser.IsInRole("GP") && !isAdmin && !isTriageNurse;

        // GPs: referrals they created. Triage nurses: own + unassigned. Admins: all.
        if (isGpOnly)
        {
            query = query.Where(r => r.CreatedByUserId == _currentUser.UserId);
        }
        else if (isTriageNurse && !isAdmin)
        {
            query = query.Where(r =>
                !r.AssignedToUserId.HasValue || r.AssignedToUserId == _currentUser.UserId);
        }

        if (!isGpOnly && !string.IsNullOrEmpty(q.AssignedTo))
        {
            var tokens = q.AssignedTo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var includeUnassigned = tokens.Any(t => t.Equals("unassigned", StringComparison.OrdinalIgnoreCase));
            var assigneeList = tokens
                .Select(id => Guid.TryParse(id, out var parsedId) ? (Guid?)parsedId : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (includeUnassigned && assigneeList.Count > 0)
            {
                query = query.Where(r =>
                    !r.AssignedToUserId.HasValue
                    || (r.AssignedToUserId.HasValue && assigneeList.Contains(r.AssignedToUserId.Value)));
            }
            else if (includeUnassigned)
            {
                query = query.Where(r => !r.AssignedToUserId.HasValue);
            }
            else if (assigneeList.Count > 0)
            {
                query = query.Where(r =>
                    r.AssignedToUserId.HasValue && assigneeList.Contains(r.AssignedToUserId.Value));
            }
        }

        List<ReferralStatus>? statusFilter = null;
        if (!string.IsNullOrEmpty(q.Status))
        {
            statusFilter = q.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.TryParse<ReferralStatus>(s, out var parsedStatus) ? (ReferralStatus?)parsedStatus : null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();
            if (statusFilter.Count == 0) statusFilter = null;
        }

        if (!string.IsNullOrEmpty(q.Urgency))
        {
            var urgencyList = q.Urgency.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(u => Enum.TryParse<UrgencyLevel>(u, out var parsedUrgency) ? (UrgencyLevel?)parsedUrgency : null)
                .Where(u => u.HasValue)
                .Select(u => u!.Value)
                .ToList();
            if (urgencyList.Any())
            {
                query = query.Where(r => urgencyList.Contains(r.Urgency));
            }
        }

        if (!string.IsNullOrEmpty(q.PatientSearch))
        {
            var cleanSearch = q.PatientSearch.Trim();
            query = query.Where(r => r.Patient != null && (r.Patient.Name.Contains(cleanSearch) || r.Patient.NhiNumber.Contains(cleanSearch)));
        }

        if (!string.IsNullOrEmpty(q.CaseNo))
        {
            var cleanCaseNo = q.CaseNo.Trim();
            query = query.Where(r => r.CaseNo.Contains(cleanCaseNo));
        }

        if (q.FromDate.HasValue)
        {
            var fromLocal = DateTime.SpecifyKind(q.FromDate.Value.Date, DateTimeKind.Local);
            query = query.Where(r => r.ReceivedAt >= fromLocal);
        }

        if (q.ToDate.HasValue)
        {
            var toLocal = DateTime.SpecifyKind(q.ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local);
            query = query.Where(r => r.ReceivedAt <= toLocal);
        }

        if (q.SlaBreach == true)
        {
            query = query.Where(r => r.SlaBreach && !r.SlaPaused);
        }

        if (q.IsMigrated.HasValue)
        {
            query = query.Where(r => r.IsMigrated == q.IsMigrated.Value);
        }

        var overallCount = await query.CountAsync(ct);
        var activeCount = await query.CountAsync(r => r.Status != ReferralStatus.Completed && r.Status != ReferralStatus.Declined, ct);
        var urgentCount = await query.CountAsync(r => r.Urgency == UrgencyLevel.Urgent && r.Status != ReferralStatus.Completed && r.Status != ReferralStatus.Declined, ct);
        var breachedCount = await query.CountAsync(r => r.SlaBreach && !r.SlaPaused && r.Status != ReferralStatus.Completed && r.Status != ReferralStatus.Declined, ct);
        var completedCount = await query.CountAsync(r => r.Status == ReferralStatus.Completed, ct);
        var declinedCount = await query.CountAsync(r => r.Status == ReferralStatus.Declined, ct);

        if (statusFilter != null)
            query = query.Where(r => statusFilter.Contains(r.Status));
        else
            query = query.Where(r => r.Status != ReferralStatus.Completed && r.Status != ReferralStatus.Declined);

        if (q.SortBy == "receivedDate")
        {
            query = query.OrderByDescending(r => r.ReceivedAt);
        }
        else
        {
            query = query.OrderByDescending(r => r.PriorityScore).ThenBy(r => r.SlaDeadline);
        }

        var totalCount = await query.CountAsync(ct);

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
                AssignedToUserId = r.AssignedToUserId,
                AssignedToUser = r.AssignedToUser == null ? null : new { r.AssignedToUser.FullName, r.AssignedToUser.Email, r.AssignedToUser.Title },
                Attachments = r.Attachments.Select(a => new { a.Id, a.FileName, a.FilePath, a.ContentType }).ToList(),
                r.ClaimedAt
            })
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return AppResult.Success(new
        {
            items = referrals,
            totalCount,
            overallCount,
            activeCount,
            urgentCount,
            breachedCount,
            completedCount,
            declinedCount,
            page,
            pageSize,
            totalPages
        });
    }

    public async Task<AppResult> GetReferralAsync(Guid id, CancellationToken ct = default)
    {
        var referral = await _db.Referrals
            .Include(r => r.CreatedByUser)
            .Include(r => r.ClaimedByUser)
            .Include(r => r.AssignedToUser)
            .Include(r => r.Patient)
            .Include(r => r.Attachments)
            .Include(r => r.AuditLogs)
                .ThenInclude(a => a.PerformedByUser)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (referral == null) return AppResult.NotFound();

        if (!CanAccessReferral(referral))
            return AppResult.Forbid();

        return AppResult.Success(referral);
    }

    public async Task<AppResult> CreateReferralAsync(CreateReferralRequest request, CancellationToken ct = default)
    {
        var weights = await GetWeights(ct);

        var patient = await _db.Patients.FindAsync(new object[] { request.PatientId }, ct);
        if (patient == null) return AppResult.BadRequest("Patient not found.");

        var caseNo = await AllocateNextCaseNoAsync(ct);

        var receivedAt = DateTime.Now;
        var referral = new Referral
        {
            PatientId = request.PatientId,
            SpecialistType = request.SpecialistType,
            Reason = request.Reason,
            Urgency = request.Urgency,
            CreatedByUserId = _currentUser.UserId,
            ReferringGPId = _currentUser.UserId.ToString(),
            ReceivedAt = receivedAt,
            SlaDeadline = Referral.CalculateSlaDeadline(request.Urgency, receivedAt),
            // Hospital queue: leave unassigned unless explicitly assigned to hospital staff.
            AssignedToUserId = request.AssignedToUserId,
            CaseNo = caseNo
        };

        referral.PriorityScore = PriorityCalculator.Calculate(
            referral.Urgency, referral.ReceivedAt, patient.DateOfBirth,
            weights.urgency, weights.waittime, weights.patient);

        _db.Referrals.Add(referral);

        _db.AuditLogs.Add(new AuditLog
        {
            ReferralId = referral.Id,
            PerformedByUserId = _currentUser.UserId,
            Action = "Created",
            ToStatus = ReferralStatus.Received
        });

        await _db.SaveChangesAsync(ct);
        await _queueNotifier.ReferralCreatedAsync(referral.Id, ct);
        return AppResult.Created(referral);
    }

    public async Task<AppResult> UpdateReferralAsync(Guid id, UpdateReferralRequest request, CancellationToken ct = default)
    {
        var referral = await _db.Referrals
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (referral == null) return AppResult.NotFound();

        if (_currentUser.IsInRole("GP") && !_currentUser.IsInRole("Admin") && !_currentUser.IsInRole("TriageNurse") && referral.CreatedByUserId != _currentUser.UserId)
            return AppResult.Forbid();

        if (request.RowVersion != null)
        {
            _db.Entry(referral).Property(r => r.RowVersion).OriginalValue = request.RowVersion;
        }

        referral.SpecialistType = request.SpecialistType;
        referral.Reason = request.Reason;
        referral.Urgency = request.Urgency;
        referral.AssignedToUserId = request.AssignedToUserId;
        referral.UpdatedAt = DateTime.Now;

        var weights = await GetWeights(ct);
        referral.SlaDeadline = Referral.CalculateSlaDeadline(referral.Urgency, referral.ReceivedAt);
        referral.EvaluateSlaBreach();
        referral.PriorityScore = PriorityCalculator.Calculate(
            referral.Urgency, referral.ReceivedAt, referral.Patient?.DateOfBirth ?? DateTime.Now,
            weights.urgency, weights.waittime, weights.patient);

        _db.AuditLogs.Add(new AuditLog
        {
            ReferralId = referral.Id,
            PerformedByUserId = _currentUser.UserId,
            Action = "Updated"
        });

        try
        {
            await _db.SaveChangesAsync(ct);
            await _queueNotifier.ReferralUpdatedAsync(referral.Id, ct);
            return AppResult.Success(new { message = "Referral updated successfully.", rowVersion = referral.RowVersion });
        }
        catch (DbUpdateConcurrencyException)
        {
            return AppResult.Conflict("This referral was recently modified by another user. Please close and reopen the form to get the latest version before saving.");
        }
    }

    public async Task<AppResult> ClaimReferralAsync(Guid id, ConcurrencyRequest req, CancellationToken ct = default)
    {
        var referral = await _db.Referrals.FindAsync(new object[] { id }, ct);
        if (referral == null) return AppResult.NotFound();

        if (!CanAccessReferral(referral))
            return AppResult.Forbid();

        _db.Entry(referral).Property(r => r.RowVersion).OriginalValue = req.RowVersion;

        try
        {
            referral.Claim(_currentUser.UserId);
            _db.AuditLogs.Add(new AuditLog
            {
                ReferralId = referral.Id,
                PerformedByUserId = _currentUser.UserId,
                Action = "Claimed"
            });
            await _db.SaveChangesAsync(ct);
            await _queueNotifier.ReferralClaimedAsync(id, _currentUser.UserId, ct);
            return AppResult.Success(new { message = "Referral claimed successfully.", rowVersion = referral.RowVersion });
        }
        catch (DbUpdateConcurrencyException)
        {
            return AppResult.Conflict("This referral was modified by another user. Please refresh and try again.");
        }
        catch (ReferralAlreadyClaimedException ex)
        {
            return AppResult.Conflict(ex.Message);
        }
    }

    public async Task<AppResult> ReleaseReferralAsync(Guid id, CancellationToken ct = default)
    {
        var referral = await _db.Referrals.FindAsync(new object[] { id }, ct);
        if (referral == null) return AppResult.NotFound();

        if (!_currentUser.IsInRole("Admin")
            && referral.ClaimedByUserId.HasValue
            && referral.ClaimedByUserId != _currentUser.UserId)
            return AppResult.Forbid();

        referral.Release();
        _db.AuditLogs.Add(new AuditLog { ReferralId = id, PerformedByUserId = _currentUser.UserId, Action = "Released" });
        await _db.SaveChangesAsync(ct);
        await _queueNotifier.ReferralReleasedAsync(id, ct);
        return AppResult.Success(new { message = "Referral released.", rowVersion = referral.RowVersion });
    }

    public async Task<AppResult> TransitionReferralAsync(Guid id, TransitionRequest req, CancellationToken ct = default)
    {
        var referral = await _db.Referrals.FindAsync(new object[] { id }, ct);
        if (referral == null) return AppResult.NotFound();

        if (!CanAccessReferral(referral))
            return AppResult.Forbid();

        try
        {
            var fromStatus = referral.Status;
            referral.TransitionTo(req.NewStatus);

            _db.AuditLogs.Add(new AuditLog
            {
                ReferralId = id,
                PerformedByUserId = _currentUser.UserId,
                Action = "StatusChanged",
                FromStatus = fromStatus,
                ToStatus = req.NewStatus,
                Notes = req.Notes
            });

            await _db.SaveChangesAsync(ct);
            await _queueNotifier.ReferralUpdatedAsync(id, ct);
            return AppResult.Success(new { message = "Status updated.", rowVersion = referral.RowVersion });
        }
        catch (DbUpdateConcurrencyException)
        {
            return AppResult.Conflict("Unable to update status: the referral was modified by another user. Please refresh and try again.");
        }
        catch (InvalidReferralTransitionException ex)
        {
            return AppResult.BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return AppResult.ServerError(new { message = "Unable to update status. Please try again." });
        }
    }

    public async Task<AppResult> PauseSlaAsync(Guid id, PauseSlaRequest req, CancellationToken ct = default)
    {
        var referral = await _db.Referrals.FindAsync(new object[] { id }, ct);
        if (referral == null) return AppResult.NotFound();

        if (!CanAccessReferral(referral))
            return AppResult.Forbid();

        if (referral.ClaimedByUserId.HasValue && referral.ClaimedByUserId != _currentUser.UserId)
            return AppResult.Conflict("This referral is claimed by another user. Claim or release it before pausing SLA.");

        _db.Entry(referral).Property(r => r.RowVersion).OriginalValue = req.RowVersion;

        try
        {
            referral.PauseSla(req.Reason ?? "WaitingOnPatient");

            _db.AuditLogs.Add(new AuditLog
            {
                ReferralId = id,
                PerformedByUserId = _currentUser.UserId,
                Action = "SlaPaused",
                Notes = referral.SlaPauseReason
            });

            await _db.SaveChangesAsync(ct);
            await _queueNotifier.ReferralUpdatedAsync(id, ct);
            return AppResult.Success(new
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
            return AppResult.Conflict("Unable to pause SLA: the referral was modified by another user. Please refresh and try again.");
        }
        catch (InvalidSlaPauseException ex)
        {
            return AppResult.BadRequest(ex.Message);
        }
    }

    public async Task<AppResult> ResumeSlaAsync(Guid id, ConcurrencyRequest req, CancellationToken ct = default)
    {
        var referral = await _db.Referrals.FindAsync(new object[] { id }, ct);
        if (referral == null) return AppResult.NotFound();

        if (!CanAccessReferral(referral))
            return AppResult.Forbid();

        if (referral.ClaimedByUserId.HasValue && referral.ClaimedByUserId != _currentUser.UserId)
            return AppResult.Conflict("This referral is claimed by another user. Claim or release it before resuming SLA.");

        _db.Entry(referral).Property(r => r.RowVersion).OriginalValue = req.RowVersion;

        try
        {
            var pausedAt = referral.SlaPausedAt;
            referral.ResumeSla();

            _db.AuditLogs.Add(new AuditLog
            {
                ReferralId = id,
                PerformedByUserId = _currentUser.UserId,
                Action = "SlaResumed",
                Notes = pausedAt.HasValue
                    ? $"Paused since {pausedAt:dd MMM yyyy HH:mm}; deadline extended to {referral.SlaDeadline:dd MMM yyyy HH:mm}"
                    : null
            });

            await _db.SaveChangesAsync(ct);
            await _queueNotifier.ReferralUpdatedAsync(id, ct);
            return AppResult.Success(new
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
            return AppResult.Conflict("Unable to resume SLA: the referral was modified by another user. Please refresh and try again.");
        }
        catch (InvalidSlaPauseException ex)
        {
            return AppResult.BadRequest(ex.Message);
        }
    }

    public async Task<AppResult> UploadAttachmentAsync(
        Guid id,
        string fileName,
        string contentType,
        long length,
        Stream content,
        CancellationToken ct = default)
    {
        var referral = await _db.Referrals.FindAsync(new object[] { id }, ct);
        if (referral == null) return AppResult.NotFound();

        if (!CanAccessReferral(referral))
            return AppResult.Forbid();

        if (length == 0)
            return AppResult.BadRequest("No file uploaded.");

        if (length > MaxPdfBytes)
            return AppResult.BadRequest("PDF attachments must be 20 MB or smaller.");

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return AppResult.BadRequest("Only PDF file attachments (.pdf) are allowed.");

        if (string.IsNullOrEmpty(contentType) || !contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return AppResult.BadRequest("Invalid file type. Only application/pdf MIME type is allowed.");

        try
        {
            var buffer = new byte[4];
            var read = await content.ReadAsync(buffer.AsMemory(0, 4), ct);
            if (read < 4 || buffer[0] != 0x25 || buffer[1] != 0x50 || buffer[2] != 0x44 || buffer[3] != 0x46)
                return AppResult.BadRequest("Security validation failed: File signature does not match PDF format.");
        }
        catch (Exception)
        {
            return AppResult.BadRequest("Failed to perform security validation on the uploaded file.");
        }

        if (content.CanSeek)
            content.Position = 0;

        var fileId = Guid.NewGuid();
        var (relativePath, _) = await _attachments.SaveAsync(fileId, fileName, content, ct);

        var attachment = new ReferralAttachment
        {
            Id = fileId,
            ReferralId = id,
            FileName = fileName,
            FilePath = relativePath,
            ContentType = contentType
        };

        _db.ReferralAttachments.Add(attachment);
        _db.AuditLogs.Add(new AuditLog { ReferralId = id, PerformedByUserId = _currentUser.UserId, Action = $"Uploaded {fileName}" });
        await _db.SaveChangesAsync(ct);

        await _queueNotifier.ReferralUpdatedAsync(referral.Id, ct);
        return AppResult.Success(attachment);
    }

    public async Task<AppResult> GetAttachmentAsync(Guid attachmentId, bool download, CancellationToken ct = default)
    {
        var attachment = await _db.ReferralAttachments
            .Include(a => a.Referral)
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);
        if (attachment?.Referral == null) return AppResult.NotFound();

        if (!CanAccessReferral(attachment.Referral))
            return AppResult.Forbid();

        if (!_attachments.Exists(attachment.FilePath))
            return AppResult.NotFound();

        _db.AuditLogs.Add(new AuditLog
        {
            ReferralId = attachment.ReferralId,
            PerformedByUserId = _currentUser.UserId,
            Action = download ? $"Downloaded {attachment.FileName}" : $"Viewed {attachment.FileName}"
        });
        await _db.SaveChangesAsync(ct);

        var fileBytes = await _attachments.ReadAsync(attachment.FilePath, ct);
        if (fileBytes == null)
            return AppResult.NotFound();

        if (download)
            return AppResult.File(fileBytes, attachment.ContentType, attachment.FileName);

        return AppResult.File(fileBytes, attachment.ContentType, inline: true);
    }

    private async Task<string> AllocateNextCaseNoAsync(CancellationToken ct)
    {
        var existing = await _db.Referrals.AsNoTracking()
            .Select(r => r.CaseNo)
            .ToListAsync(ct);
        return CaseNoGenerator.Next(existing);
    }

    private bool CanAccessReferral(Referral referral)
    {
        if (_currentUser.IsInRole("Admin"))
            return true;

        // Triage nurses: unassigned queue items plus their own assigned caseload.
        if (_currentUser.IsInRole("TriageNurse"))
            return !referral.AssignedToUserId.HasValue
                || referral.AssignedToUserId == _currentUser.UserId;

        // GPs: referrals they created (referring clinician view).
        return referral.CreatedByUserId == _currentUser.UserId;
    }

    private async Task SyncSlaBreachesAsync(CancellationToken ct)
    {
        var now = DateTime.Now;
        var overdue = await _db.Referrals
            .Where(r => r.Status == ReferralStatus.Received && !r.SlaBreach && !r.SlaPaused && r.SlaDeadline < now)
            .ToListAsync(ct);

        if (overdue.Count == 0) return;

        foreach (var referral in overdue)
            referral.EvaluateSlaBreach(now);

        await _db.SaveChangesAsync(ct);
    }

    private async Task<(double urgency, double waittime, double patient)> GetWeights(CancellationToken ct)
    {
        var configs = await _db.SystemConfigs.ToListAsync(ct);
        double Get(string key, double def) =>
            double.TryParse(configs.FirstOrDefault(c => c.Key == key)?.Value, out var v) ? v : def;
        return (Get("weight_urgency", 50), Get("weight_waittime", 30), Get("weight_patient", 20));
    }
}
