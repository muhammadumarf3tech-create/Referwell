using Microsoft.EntityFrameworkCore;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Application.Common.Models;
using ReferWell.Domain.Services;

namespace ReferWell.Application.Config;

public class ConfigService : IConfigService
{
    private readonly IApplicationDbContext _db;
    private readonly IQueueNotifier _queueNotifier;
    private readonly ISecurityAuditLogger _audit;
    private readonly ICurrentUser _currentUser;

    public ConfigService(
        IApplicationDbContext db,
        IQueueNotifier queueNotifier,
        ISecurityAuditLogger audit,
        ICurrentUser currentUser)
    {
        _db = db;
        _queueNotifier = queueNotifier;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<AppResult> GetWeightsAsync(CancellationToken ct = default)
    {
        var configs = await _db.SystemConfigs
            .Where(c => c.Key.StartsWith("weight_"))
            .ToListAsync(ct);
        return AppResult.Success(configs.Select(c => new { c.Key, c.Value, c.Description }));
    }

    public async Task<AppResult> UpdateWeightsAsync(UpdateWeightsRequest request, CancellationToken ct = default)
    {
        var total = request.WeightUrgency + request.WeightWaittime + request.WeightPatient;
        if (Math.Abs(total - 100) > 0.01)
            return AppResult.BadRequest("Weights must sum to 100%.");

        var configs = await _db.SystemConfigs.ToListAsync(ct);
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
            .ToListAsync(ct);

        foreach (var r in referrals)
        {
            r.PriorityScore = PriorityCalculator.Calculate(
                r.Urgency, r.ReceivedAt, r.Patient?.DateOfBirth ?? DateTime.Now,
                request.WeightUrgency, request.WeightWaittime, request.WeightPatient);
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "PriorityWeightsUpdated",
            _currentUser.UserId,
            details: $"urgency={request.WeightUrgency}, wait={request.WeightWaittime}, patient={request.WeightPatient}",
            ct: ct);

        await _queueNotifier.QueueResortedAsync(
            request.WeightUrgency,
            request.WeightWaittime,
            request.WeightPatient,
            ct);

        return AppResult.Success(new { message = "Weights updated and queue recalculated." });
    }
}
