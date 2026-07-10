using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Infrastructure.Data;
using ReferWell.Infrastructure.Services;
using System.Security.Claims;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TriageNurse")]
public class MassCommController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MassCommChannel _channel;

    public MassCommController(AppDbContext db, MassCommChannel channel)
    {
        _db = db;
        _channel = channel;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

    [HttpGet]
    public async Task<IActionResult> GetCampaigns()
    {
        var campaigns = await _db.MassCommCampaigns
            .Include(c => c.CreatedByUser)
            .Include(c => c.Messages)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id, c.Name, c.Status, c.CreatedAt,
                CreatedByUser = c.CreatedByUser == null ? null : new { c.CreatedByUser.FullName },
                TotalMessages = c.Messages.Count,
                SentMessages = c.Messages.Count(m => m.Status == "Sent"),
                FailedMessages = c.Messages.Count(m => m.Status == "Failed")
            })
            .ToListAsync();
        return Ok(campaigns);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest req)
    {
        // Resolve recipients based on filter
        var referrals = await _db.Referrals
            .Include(r => r.CreatedByUser)
            .Include(r => r.Patient)
            .Where(r => string.IsNullOrEmpty(req.UrgencyFilter)
                     || r.Urgency == Enum.Parse<UrgencyLevel>(req.UrgencyFilter))
            .Where(r => string.IsNullOrEmpty(req.StatusFilter)
                     || r.Status == Enum.Parse<ReferralStatus>(req.StatusFilter))
            .ToListAsync();

        var campaign = new MassCommCampaign
        {
            Name = req.Name,
            SubjectTemplate = req.SubjectTemplate,
            BodyTemplate = req.BodyTemplate,
            FilterCriteria = $"urgency={req.UrgencyFilter},status={req.StatusFilter}",
            CreatedByUserId = CurrentUserId,
            Status = "Sending"
        };

        foreach (var r in referrals)
        {
            var patientName = r.Patient?.Name ?? string.Empty;
            var body = req.BodyTemplate
                .Replace("{PatientName}", patientName)
                .Replace("{SpecialistType}", r.SpecialistType)
                .Replace("{Status}", r.Status.ToString())
                .Replace("{SlaDeadline}", r.SlaDeadline.ToString("dd MMM yyyy HH:mm"));

            campaign.Messages.Add(new MassCommMessage
            {
                RecipientEmail = r.CreatedByUser?.Email ?? "unknown@referwell.com",
                RecipientName = patientName,
                RenderedBody = body
            });
        }

        _db.MassCommCampaigns.Add(campaign);
        await _db.SaveChangesAsync();

        // Enqueue for throttled background processing
        await _channel.Writer.WriteAsync(new MassCommJob(campaign.Id, campaign.Messages.ToList()));

        return Ok(new { campaignId = campaign.Id, messageCount = campaign.Messages.Count });
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(Guid id)
    {
        var messages = await _db.MassCommMessages
            .Where(m => m.CampaignId == id)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
        return Ok(messages);
    }
}

public record CreateCampaignRequest(
    string Name,
    string SubjectTemplate,
    string BodyTemplate,
    string? UrgencyFilter,
    string? StatusFilter);
