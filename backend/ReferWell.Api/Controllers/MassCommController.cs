using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReferWell.Api.Authorization;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Infrastructure.Data;
using ReferWell.Infrastructure.Services;
using System.Security.Claims;
using System.Text.Json;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[MenuAuthorize("Mass Communications")]
public class MassCommController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MassCommChannel _channel;
    private readonly SecurityAuditService _audit;

    public MassCommController(AppDbContext db, MassCommChannel channel, SecurityAuditService audit)
    {
        _db = db;
        _channel = channel;
        _audit = audit;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

    [HttpGet]
    public async Task<IActionResult> GetCampaigns(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15)
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

        var totalCount = await query.CountAsync();
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
            .ToListAsync();

        return Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions()
    {
        var specialistTypes = await _db.Referrals.Select(r => r.SpecialistType).Distinct().OrderBy(x => x).ToListAsync();
        var assignees = await _db.Users
            .Where(u => _db.Referrals.Any(r => r.AssignedToUserId == u.Id))
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync();
        return Ok(new { specialistTypes, assignees });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.BodyTemplate))
            return BadRequest(new { message = "Campaign name and message body are required." });

        var validationError = ValidateTemplate(req.SubjectTemplate, req.BodyTemplate);
        if (validationError != null) return BadRequest(new { message = validationError });

        var recipients = await BuildRecipients(req);
        if (recipients.Count == 0)
            return BadRequest(new { message = "No referrals match the selected filters with a valid recipient email." });

        var campaign = new MassCommCampaign
        {
            Name = req.Name,
            SubjectTemplate = req.SubjectTemplate,
            BodyTemplate = req.BodyTemplate,
            FilterCriteria = JsonSerializer.Serialize(req.Filters),
            CreatedByUserId = CurrentUserId,
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
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            "MassCommCampaignCreated",
            CurrentUserId,
            details: $"Campaign '{campaign.Name}' with {campaign.Messages.Count} messages");

        // Enqueue for throttled background processing
        await _channel.Writer.WriteAsync(new MassCommJob(campaign.Id, campaign.Messages.ToList()));

        return Ok(new { campaignId = campaign.Id, messageCount = campaign.Messages.Count });
    }

    [HttpPost("preview")]
    public async Task<IActionResult> PreviewCampaign([FromBody] CreateCampaignRequest req)
    {
        var validationError = ValidateTemplate(req.SubjectTemplate, req.BodyTemplate);
        if (validationError != null) return BadRequest(new { message = validationError });

        var recipients = await BuildRecipients(req);
        return Ok(new
        {
            totalCount = recipients.Count,
            recipients = recipients.Take(100).Select(r => new
            {
                r.Name, r.Email, r.Referral.CaseNo, PatientName = r.Referral.Patient?.Name,
                Status = r.Referral.Status.ToString(), r.Subject, r.Body
            })
        });
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(Guid id)
    {
        var messages = await _db.MassCommMessages
            .Where(m => m.CampaignId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id, m.RecipientName, m.RecipientEmail, m.RecipientType, m.ReferralCaseNo,
                m.RenderedSubject, m.RenderedBody, m.Status, m.ErrorMessage, m.SentAt, m.CreatedAt
            })
            .ToListAsync();
        return Ok(messages);
    }

    private async Task<List<ResolvedRecipient>> BuildRecipients(CreateCampaignRequest req)
    {
        var filters = req.Filters ?? new CampaignFilters();

        // Match referral grid: sync overdue Received referrals before applying SlaBreach filter
        if (filters.OnlySlaBreached == true)
            await SyncSlaBreachesAsync();

        var query = _db.Referrals
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

        var referrals = await query.OrderBy(r => r.Patient!.Name).ToListAsync();
        return referrals.Select(r =>
        {
            var isGp = string.Equals(req.RecipientType, "ReferringGP", StringComparison.OrdinalIgnoreCase);
            // Referring GP notifications go to the assigned clinician (not the submitter)
            var name = isGp ? r.AssignedToUser?.FullName : r.Patient?.Name;
            var email = isGp ? r.AssignedToUser?.Email : r.Patient?.Email;
            return new ResolvedRecipient(r, name ?? string.Empty, email ?? string.Empty,
                RenderTemplate(req.SubjectTemplate, r), RenderTemplate(req.BodyTemplate, r));
        }).Where(r => !string.IsNullOrWhiteSpace(r.Email)).ToList();
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
            .Replace("{ReferringGPName}", referral.AssignedToUser?.FullName ?? string.Empty);

    private record ResolvedRecipient(Referral Referral, string Name, string Email, string Subject, string Body);
}

public record CreateCampaignRequest(
    string Name,
    string SubjectTemplate,
    string BodyTemplate,
    string RecipientType,
    CampaignFilters? Filters);

public record CampaignFilters(
    List<string>? Urgencies = null,
    List<string>? Statuses = null,
    List<string>? SpecialistTypes = null,
    List<string>? AssignedToUserIds = null,
    bool? OnlySlaBreached = null,
    DateTime? ReceivedFrom = null,
    DateTime? ReceivedTo = null,
    string? CaseNo = null);
