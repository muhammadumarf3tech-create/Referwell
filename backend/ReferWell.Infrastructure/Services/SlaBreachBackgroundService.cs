using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReferWell.Domain.Enums;
using ReferWell.Infrastructure.Data;
using ReferWell.Infrastructure.Hubs;

namespace ReferWell.Infrastructure.Services;

/// <summary>
/// Periodically marks Received referrals past their SLA deadline as breached
/// and pushes real-time alerts to the referral queue.
/// </summary>
public class SlaBreachBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaBreachBackgroundService> _logger;

    public SlaBreachBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SlaBreachBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MarkBreachesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SLA breach scanner failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task MarkBreachesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<QueueHub>>();

        var now = DateTime.Now;
        var candidates = await db.Referrals
            .Include(r => r.Patient)
            .Where(r => r.Status == ReferralStatus.Received && !r.SlaBreach && !r.SlaPaused && r.SlaDeadline < now)
            .ToListAsync(stoppingToken);

        if (candidates.Count == 0) return;

        var newlyBreached = new List<object>();
        foreach (var referral in candidates)
        {
            if (!referral.EvaluateSlaBreach(now)) continue;
            newlyBreached.Add(new
            {
                referral.Id,
                referral.CaseNo,
                PatientName = referral.Patient?.Name ?? string.Empty,
                Urgency = referral.Urgency.ToString(),
                referral.SlaDeadline
            });
        }

        if (newlyBreached.Count == 0) return;

        await db.SaveChangesAsync(stoppingToken);

        foreach (var item in newlyBreached)
        {
            await hub.Clients.Group("QueueGroup").SendAsync("SlaBreached", item, stoppingToken);
        }

        _logger.LogWarning("Marked {Count} referral(s) as SLA breached", newlyBreached.Count);
    }
}
