using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ReferWell.Api.Authorization;
using ReferWell.Domain.Services;
using ReferWell.Infrastructure.Data;
using ReferWell.Infrastructure.Hubs;
using ReferWell.Infrastructure.Services;
using System.Security.Claims;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[MenuAuthorize("Priority Config")]
public class ConfigController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<QueueHub> _hub;
    private readonly SecurityAuditService _audit;

    public ConfigController(AppDbContext db, IHubContext<QueueHub> hub, SecurityAuditService audit)
    {
        _db = db;
        _hub = hub;
        _audit = audit;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

    [HttpGet("weights")]
    public async Task<IActionResult> GetWeights()
    {
        var configs = await _db.SystemConfigs
            .Where(c => c.Key.StartsWith("weight_"))
            .ToListAsync();
        return Ok(configs.Select(c => new { c.Key, c.Value, c.Description }));
    }

    [HttpPost("weights")]
    public async Task<IActionResult> UpdateWeights([FromBody] UpdateWeightsRequest request)
    {
        // Validate weights sum to 100 (also enforced by FluentValidation)
        var total = request.WeightUrgency + request.WeightWaittime + request.WeightPatient;
        if (Math.Abs(total - 100) > 0.01)
            return BadRequest(new { message = "Weights must sum to 100%." });

        var configs = await _db.SystemConfigs.ToListAsync();
        void SetConfig(string key, double value)
        {
            var c = configs.FirstOrDefault(x => x.Key == key);
            if (c != null) { c.Value = value.ToString(); c.UpdatedAt = DateTime.Now; }
        }

        SetConfig("weight_urgency", request.WeightUrgency);
        SetConfig("weight_waittime", request.WeightWaittime);
        SetConfig("weight_patient", request.WeightPatient);

        var referrals = await _db.Referrals
            .Include(r => r.Patient)
            .Where(r => r.Status != Domain.Enums.ReferralStatus.Completed
                     && r.Status != Domain.Enums.ReferralStatus.Declined)
            .ToListAsync();

        foreach (var r in referrals)
        {
            r.PriorityScore = PriorityCalculator.Calculate(
                r.Urgency, r.ReceivedAt, r.Patient?.DateOfBirth ?? DateTime.Now,
                request.WeightUrgency, request.WeightWaittime, request.WeightPatient);
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            "PriorityWeightsUpdated",
            CurrentUserId,
            details: $"urgency={request.WeightUrgency}, wait={request.WeightWaittime}, patient={request.WeightPatient}");

        await _hub.Clients.Group("QueueGroup").SendAsync("QueueResorted", new
        {
            weightUrgency = request.WeightUrgency,
            weightWaittime = request.WeightWaittime,
            weightPatient = request.WeightPatient
        });

        return Ok(new { message = "Weights updated and queue recalculated." });
    }
}

public record UpdateWeightsRequest(double WeightUrgency, double WeightWaittime, double WeightPatient);
