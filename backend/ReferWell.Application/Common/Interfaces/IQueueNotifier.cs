namespace ReferWell.Application.Common.Interfaces;

public interface IQueueNotifier
{
    Task ReferralCreatedAsync(Guid referralId, CancellationToken ct = default);
    Task ReferralUpdatedAsync(Guid referralId, CancellationToken ct = default);
    Task ReferralClaimedAsync(Guid referralId, Guid claimedByUserId, CancellationToken ct = default);
    Task ReferralReleasedAsync(Guid referralId, CancellationToken ct = default);
    Task QueueResortedAsync(double weightUrgency, double weightWaittime, double weightPatient, CancellationToken ct = default);
}
