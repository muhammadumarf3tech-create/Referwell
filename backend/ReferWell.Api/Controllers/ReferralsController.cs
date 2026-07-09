using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Domain.Exceptions;
using ReferWell.Domain.Services;
using ReferWell.Infrastructure.Data;
using ReferWell.Infrastructure.Hubs;
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
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role)!;

    // ── GET /api/referrals ────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetReferrals([FromQuery] string? status, [FromQuery] string? urgency)
    {
        var query = _db.Referrals
            .Include(r => r.CreatedByUser)
            .Include(r => r.ClaimedByUser)
            .AsQueryable();

        // RBAC: GPs only see their own referrals
        if (CurrentRole == "GP")
            query = query.Where(r => r.CreatedByUserId == CurrentUserId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReferralStatus>(status, out var parsedStatus))
            query = query.Where(r => r.Status == parsedStatus);

        if (!string.IsNullOrEmpty(urgency) && Enum.TryParse<UrgencyLevel>(urgency, out var parsedUrgency))
            query = query.Where(r => r.Urgency == parsedUrgency);

        var referrals = await query
            .OrderByDescending(r => r.PriorityScore)
            .ThenBy(r => r.SlaDeadline)
            .Select(r => new
            {
                r.Id,
                r.PatientName,
                r.PatientDateOfBirth,
                r.SpecialistType,
                r.Reason,
                Urgency = r.Urgency.ToString(),
                Status = r.Status.ToString(),
                r.PriorityScore,
                r.ReceivedAt,
                r.SlaDeadline,
                r.SlaBreach,
                r.CreatedAt,
                r.RowVersion,
                CreatedByUser = r.CreatedByUser == null ? null : new { r.CreatedByUser.FullName, r.CreatedByUser.Email },
                ClaimedByUser = r.ClaimedByUser == null ? null : new { r.ClaimedByUser.FullName, r.ClaimedByUser.Email },
                r.ClaimedAt
            })
            .ToListAsync();

        return Ok(referrals);
    }

    // ── GET /api/referrals/{id} ───────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReferral(Guid id)
    {
        var referral = await _db.Referrals
            .Include(r => r.CreatedByUser)
            .Include(r => r.ClaimedByUser)
            .Include(r => r.AuditLogs)
                .ThenInclude(a => a.PerformedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (referral == null) return NotFound();

        // RBAC check for GPs
        if (CurrentRole == "GP" && referral.CreatedByUserId != CurrentUserId)
            return Forbid();

        return Ok(referral);
    }

    // ── POST /api/referrals ───────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "GP,Admin")]
    public async Task<IActionResult> CreateReferral([FromBody] CreateReferralRequest request)
    {
        var weights = await GetWeights();

        var receivedAt = DateTime.UtcNow;
        var referral = new Referral
        {
            PatientName = request.PatientName,
            PatientDateOfBirth = request.PatientDateOfBirth,
            SpecialistType = request.SpecialistType,
            Reason = request.Reason,
            Urgency = request.Urgency,
            CreatedByUserId = CurrentUserId,
            ReferringGPId = CurrentUserId.ToString(),
            ReceivedAt = receivedAt,
            SlaDeadline = Referral.CalculateSlaDeadline(request.Urgency, receivedAt)
        };

        referral.PriorityScore = PriorityCalculator.Calculate(
            referral.Urgency, referral.ReceivedAt, referral.PatientDateOfBirth,
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

    // ── POST /api/referrals/{id}/claim ────────────────────────────────────────
    [HttpPost("{id}/claim")]
    [Authorize(Roles = "TriageNurse,Admin")]
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
    [Authorize(Roles = "TriageNurse,Admin")]
    public async Task<IActionResult> ReleaseReferral(Guid id)
    {
        var referral = await _db.Referrals.FindAsync(id);
        if (referral == null) return NotFound();

        referral.Release();
        _db.AuditLogs.Add(new AuditLog { ReferralId = id, PerformedByUserId = CurrentUserId, Action = "Released" });
        await _db.SaveChangesAsync();
        await _hub.Clients.Group("QueueGroup").SendAsync("ReferralReleased", id);
        return Ok();
    }

    // ── POST /api/referrals/{id}/transition ───────────────────────────────────
    [HttpPost("{id}/transition")]
    [Authorize(Roles = "TriageNurse,Admin")]
    public async Task<IActionResult> TransitionReferral(Guid id, [FromBody] TransitionRequest req)
    {
        var referral = await _db.Referrals.FindAsync(id);
        if (referral == null) return NotFound();

        _db.Entry(referral).Property(r => r.RowVersion).OriginalValue = req.RowVersion;

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
            return Conflict(new { message = "Concurrency conflict. Please refresh." });
        }
        catch (InvalidReferralTransitionException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<(double urgency, double waittime, double patient)> GetWeights()
    {
        var configs = await _db.SystemConfigs.ToListAsync();
        double Get(string key, double def) =>
            double.TryParse(configs.FirstOrDefault(c => c.Key == key)?.Value, out var v) ? v : def;
        return (Get("weight_urgency", 50), Get("weight_waittime", 30), Get("weight_patient", 20));
    }
}

public record CreateReferralRequest(
    string PatientName, DateTime PatientDateOfBirth, string SpecialistType,
    string Reason, UrgencyLevel Urgency);

public record ConcurrencyRequest(byte[] RowVersion);
public record TransitionRequest(ReferralStatus NewStatus, byte[] RowVersion, string? Notes);
