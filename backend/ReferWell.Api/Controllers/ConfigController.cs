using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ReferWell.Domain.Services;
using ReferWell.Infrastructure.Data;
using ReferWell.Infrastructure.Hubs;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TriageNurse")]
public class ConfigController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<QueueHub> _hub;

    public ConfigController(AppDbContext db, IHubContext<QueueHub> hub)
    {
        _db = db;
        _hub = hub;
    }

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
        // Validate weights sum to 100
        var total = request.WeightUrgency + request.WeightWaittime + request.WeightPatient;
        if (Math.Abs(total - 100) > 0.01)
            return BadRequest(new { message = "Weights must sum to 100%." });

        var configs = await _db.SystemConfigs.ToListAsync();
        void SetConfig(string key, double value)
        {
            var c = configs.FirstOrDefault(x => x.Key == key);
            if (c != null) { c.Value = value.ToString(); c.UpdatedAt = DateTime.UtcNow; }
        }

        SetConfig("weight_urgency", request.WeightUrgency);
        SetConfig("weight_waittime", request.WeightWaittime);
        SetConfig("weight_patient", request.WeightPatient);

        // Recalculate all active referral scores
        var referrals = await _db.Referrals
            .Where(r => r.Status != Domain.Enums.ReferralStatus.Completed
                     && r.Status != Domain.Enums.ReferralStatus.Declined)
            .ToListAsync();

        foreach (var r in referrals)
        {
            r.PriorityScore = PriorityCalculator.Calculate(
                r.Urgency, r.ReceivedAt, r.PatientDateOfBirth,
                request.WeightUrgency, request.WeightWaittime, request.WeightPatient);
        }

        await _db.SaveChangesAsync();

        // Broadcast queue resorted event to all clients
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
