using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Domain.Enums;
using ReferWell.Domain.Services;
using ReferWell.Infrastructure.Data;

namespace ReferWell.Infrastructure.Services;

/// <summary>
/// Periodically recalculates active referral priority scores so wait time
/// continues to affect queue ordering as referrals age.
/// </summary>
public class PriorityScoreRefreshBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PriorityScoreRefreshBackgroundService> _logger;

    public PriorityScoreRefreshBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PriorityScoreRefreshBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshPriorityScoresAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Priority score refresh failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task RefreshPriorityScoresAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var queueNotifier = scope.ServiceProvider.GetRequiredService<IQueueNotifier>();

        var configs = await db.SystemConfigs
            .Where(c => c.Key.StartsWith("weight_"))
            .ToListAsync(stoppingToken);

        double GetWeight(string key, double defaultValue) =>
            double.TryParse(configs.FirstOrDefault(c => c.Key == key)?.Value, out var value) ? value : defaultValue;

        var weightUrgency = GetWeight("weight_urgency", 50);
        var weightWaittime = GetWeight("weight_waittime", 30);
        var weightPatient = GetWeight("weight_patient", 20);
        var now = DateTime.Now;

        var referrals = await db.Referrals
            .Include(r => r.Patient)
            .Where(r => r.Status != ReferralStatus.Completed && r.Status != ReferralStatus.Declined)
            .ToListAsync(stoppingToken);

        if (referrals.Count == 0) return;

        var updatedCount = 0;
        foreach (var referral in referrals)
        {
            var recalculatedScore = PriorityCalculator.Calculate(
                referral.Urgency,
                referral.ReceivedAt,
                referral.Patient?.DateOfBirth ?? now,
                weightUrgency,
                weightWaittime,
                weightPatient,
                now);

            if (Math.Abs(referral.PriorityScore - recalculatedScore) < 0.0001)
                continue;

            referral.PriorityScore = recalculatedScore;
            updatedCount++;
        }

        if (updatedCount == 0) return;

        await db.SaveChangesAsync(stoppingToken);
        await queueNotifier.QueueResortedAsync(weightUrgency, weightWaittime, weightPatient, stoppingToken);

        _logger.LogInformation(
            "Refreshed priority scores for {UpdatedCount} active referral(s)",
            updatedCount);
    }
}
