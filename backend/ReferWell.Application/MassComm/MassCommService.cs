using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Application.Common.Models;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;

namespace ReferWell.Application.MassComm;

public class MassCommService : IMassCommService
{
    private readonly IApplicationDbContext _db;
    private readonly IMassCommQueue _massCommQueue;
    private readonly ISecurityAuditLogger _audit;
    private readonly ICurrentUser _currentUser;

    public MassCommService(
        IApplicationDbContext db,
        IMassCommQueue massCommQueue,
        ISecurityAuditLogger audit,
        ICurrentUser currentUser)
    {
        _db = db;
        _massCommQueue = massCommQueue;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<AppResult> GetCampaignsAsync(
        string? search = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 15,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 15;
        if (pageSize > 100) pageSize = 100;

        var query = _db.MassCommCampaigns.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(term)
                || c.Status.Contains(term)
                || (c.CreatedByUser != null && c.CreatedByUser.FullName.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        if (fromDate.HasValue)
        {
            var fromLocal = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Local);
            query = query.Where(c => c.CreatedAt >= fromLocal);
        }

        if (toDate.HasValue)
        {
            var toLocalExclusive = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Local);
            query = query.Where(c => c.CreatedAt < toLocalExclusive);
        }

        var totalCount = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id, c.Name, c.Status, c.CreatedAt,
                CreatedByUser = c.CreatedByUser == null ? null : new { c.CreatedByUser.FullName },
                TotalMessages = c.Messages.Count,
                SentMessages = c.Messages.Count(m => m.Status == "Sent"),
                FailedMessages = c.Messages.Count(m => m.Status == "Failed")
            })
            .ToListAsync(ct);

        return AppResult.Success(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<AppResult> GetFilterOptionsAsync(CancellationToken ct = default)
    {
        var specialistTypes = await _db.Referrals.Select(r => r.SpecialistType).Distinct().OrderBy(x => x).ToListAsync(ct);
        var assignees = await _db.Users
            .Where(u => _db.Referrals.Any(r => r.AssignedToUserId == u.Id))
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);
        return AppResult.Success(new { specialistTypes, assignees });
    }

    public async Task<AppResult> CreateCampaignAsync(CreateCampaignRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.BodyTemplate))
            return AppResult.BadRequest("Campaign name and message body are required.");

        var validationError = ValidateTemplate(req.SubjectTemplate, req.BodyTemplate);
        if (validationError != null) return AppResult.BadRequest(validationError);

        var recipients = await BuildRecipients(req, ct);
        if (recipients.Count == 0)
            return AppResult.BadRequest("No referrals match the selected filters with a valid recipient email.");

        var campaign = new MassCommCampaign
        {
            Name = req.Name,
            SubjectTemplate = req.SubjectTemplate,
            BodyTemplate = req.BodyTemplate,
            FilterCriteria = JsonSerializer.Serialize(req.Filters),
            CreatedByUserId = _currentUser.UserId,
            Status = "Sending"
        };

        foreach (var recipient in recipients)
        {
            campaign.Messages.Add(new MassCommMessage
            {
                RecipientEmail = recipient.Email,
                RecipientName = recipient.Name,
                RecipientType = req.RecipientType,
                ReferralId = recipient.Referral.Id,
                ReferralCaseNo = recipient.Referral.CaseNo,
                RenderedSubject = recipient.Subject,
                RenderedBody = recipient.Body
            });
        }

        _db.MassCommCampaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "MassCommCampaignCreated",
            _currentUser.UserId,
            details: $"Campaign '{campaign.Name}' with {campaign.Messages.Count} messages",
            ct: ct);

        await _massCommQueue.EnqueueAsync(campaign.Id, campaign.Messages.ToList(), ct);

        return AppResult.Success(new { campaignId = campaign.Id, messageCount = campaign.Messages.Count });
    }

    public async Task<AppResult> PreviewCampaignAsync(CreateCampaignRequest req, CancellationToken ct = default)
    {
        var validationError = ValidateTemplate(req.SubjectTemplate, req.BodyTemplate);
        if (validationError != null) return AppResult.BadRequest(validationError);

        var recipients = await BuildRecipients(req, ct);
        return AppResult.Success(new
        {
            totalCount = recipients.Count,
            recipients = recipients.Take(100).Select(r => new
            {
                r.Name, r.Email, r.Referral.CaseNo, PatientName = r.Referral.Patient?.Name,
                Status = r.Referral.Status.ToString(), r.Subject, r.Body
            })
        });
    }

    public async Task<AppResult> GetMessagesAsync(Guid id, CancellationToken ct = default)
    {
        var messages = await _db.MassCommMessages
            .Where(m => m.CampaignId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id, m.RecipientName, m.RecipientEmail, m.RecipientType, m.ReferralCaseNo,
                m.RenderedSubject, m.RenderedBody, m.Status, m.ErrorMessage, m.SentAt, m.CreatedAt
            })
            .ToListAsync(ct);
        return AppResult.Success(messages);
    }

    private async Task<List<ResolvedRecipient>> BuildRecipients(CreateCampaignRequest req, CancellationToken ct)
    {
        var filters = req.Filters ?? new CampaignFilters();

        // Match referral grid: sync overdue Received referrals before applying SlaBreach filter
        if (filters.OnlySlaBreached == true)
            await SyncSlaBreachesAsync(ct);

        var query = _db.Referrals
            .Include(r => r.CreatedByUser)
            .Include(r => r.AssignedToUser)
            .Include(r => r.Patient)
            .AsQueryable();

        var urgencies = ParseEnumValues<UrgencyLevel>(filters.Urgencies);
        var statuses = ParseEnumValues<ReferralStatus>(filters.Statuses);
        if (urgencies.Count > 0) query = query.Where(r => urgencies.Contains(r.Urgency));
        if (statuses.Count > 0) query = query.Where(r => statuses.Contains(r.Status));
        if (filters.SpecialistTypes?.Count > 0) query = query.Where(r => filters.SpecialistTypes.Contains(r.SpecialistType));
        if (filters.AssignedToUserIds?.Count > 0)
        {
            // Always apply when the client sent assignee filters. An empty/invalid ID list must
            // match nothing — never fall through to "all referrals".
            var assignees = filters.AssignedToUserIds
                .Where(x => !string.IsNullOrWhiteSpace(x) && Guid.TryParse(x, out _))
                .Select(Guid.Parse)
                .Distinct()
                .ToList();
            query = query.Where(r => r.AssignedToUserId.HasValue && assignees.Contains(r.AssignedToUserId.Value));
        }
        // Active breaches only — paused (waiting on patient) are excluded
        if (filters.OnlySlaBreached == true) query = query.Where(r => r.SlaBreach && !r.SlaPaused);
        if (filters.ReceivedFrom.HasValue) query = query.Where(r => r.ReceivedAt >= filters.ReceivedFrom.Value.Date);
        if (filters.ReceivedTo.HasValue) query = query.Where(r => r.ReceivedAt < filters.ReceivedTo.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(filters.CaseNo)) query = query.Where(r => r.CaseNo.Contains(filters.CaseNo.Trim()));

        var referrals = await query.OrderBy(r => r.Patient!.Name).ToListAsync(ct);
        return referrals.Select(r =>
        {
            var isGp = string.Equals(req.RecipientType, "ReferringGP", StringComparison.OrdinalIgnoreCase);
            // Referring GP = submitting clinician (CreatedBy), not hospital-side AssignedTo.
            var referringGp = r.CreatedByUser;
            var name = isGp ? FormatUserName(referringGp) : r.Patient?.Name;
            var email = isGp ? referringGp?.Email : r.Patient?.Email;
            return new ResolvedRecipient(r, name ?? string.Empty, email ?? string.Empty,
                RenderTemplate(req.SubjectTemplate, r), RenderTemplate(req.BodyTemplate, r));
        }).Where(r => !string.IsNullOrWhiteSpace(r.Email)).ToList();
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

    private static List<T> ParseEnumValues<T>(IEnumerable<string>? values) where T : struct, Enum =>
        values?.Select(v => Enum.TryParse<T>(v, true, out var parsed) ? (T?)parsed : null)
            .Where(v => v.HasValue).Select(v => v!.Value).Distinct().ToList() ?? [];

    private static string? ValidateTemplate(string subject, string body)
    {
        var knownFields = new[] { "{PatientName}", "{CaseNo}", "{SpecialistType}", "{Status}", "{Urgency}", "{SlaDeadline}", "{ReceivedDate}", "{ReferringGPName}" };
        var fields = System.Text.RegularExpressions.Regex.Matches(subject + body, @"\{[^{}]+\}").Select(m => m.Value).Distinct();
        var invalid = fields.FirstOrDefault(field => !knownFields.Contains(field));
        return invalid == null ? null : $"Unsupported merge field: {invalid}";
    }

    private static string RenderTemplate(string template, Referral referral) =>
        template.Replace("{PatientName}", referral.Patient?.Name ?? string.Empty)
            .Replace("{CaseNo}", referral.CaseNo)
            .Replace("{SpecialistType}", referral.SpecialistType)
            .Replace("{Status}", referral.Status.ToString())
            .Replace("{Urgency}", referral.Urgency == UrgencyLevel.SemiUrgent ? "Semi-Urgent" : referral.Urgency.ToString())
            .Replace("{SlaDeadline}", referral.SlaDeadline.ToString("dd MMM yyyy HH:mm"))
            .Replace("{ReceivedDate}", referral.ReceivedAt.ToString("dd MMM yyyy"))
            .Replace("{ReferringGPName}", FormatUserName(referral.CreatedByUser));

    private static string FormatUserName(ApplicationUser? user)
    {
        if (user == null) return string.Empty;
        return string.IsNullOrWhiteSpace(user.Title)
            ? user.FullName
            : $"{user.Title} {user.FullName}";
    }

    private record ResolvedRecipient(Referral Referral, string Name, string Email, string Subject, string Body);
}
