using Microsoft.AspNetCore.SignalR;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Infrastructure.Hubs;

namespace ReferWell.Infrastructure.Services;

public class QueueNotifier : IQueueNotifier
{
    private readonly IHubContext<QueueHub> _hub;

    public QueueNotifier(IHubContext<QueueHub> hub)
    {
        _hub = hub;
    }

    public Task ReferralCreatedAsync(Guid referralId, CancellationToken ct = default) =>
        _hub.Clients.Group("QueueGroup").SendAsync("ReferralCreated", referralId, ct);

    public Task ReferralUpdatedAsync(Guid referralId, CancellationToken ct = default) =>
        _hub.Clients.Group("QueueGroup").SendAsync("ReferralUpdated", referralId, ct);

    public Task ReferralClaimedAsync(Guid referralId, Guid claimedByUserId, CancellationToken ct = default) =>
        _hub.Clients.Group("QueueGroup").SendAsync("ReferralClaimed", new { id = referralId, claimedBy = claimedByUserId }, ct);

    public Task ReferralReleasedAsync(Guid referralId, CancellationToken ct = default) =>
        _hub.Clients.Group("QueueGroup").SendAsync("ReferralReleased", referralId, ct);

    public Task QueueResortedAsync(double weightUrgency, double weightWaittime, double weightPatient, CancellationToken ct = default) =>
        _hub.Clients.Group("QueueGroup").SendAsync("QueueResorted", new
        {
            weightUrgency,
            weightWaittime,
            weightPatient
        }, ct);
}
