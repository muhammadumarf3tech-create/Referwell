namespace ReferWell.Application.Common.Interfaces;

public interface IMassCommQueue
{
    Task EnqueueAsync(Guid campaignId, IReadOnlyList<ReferWell.Domain.Entities.MassCommMessage> messages, CancellationToken ct = default);
}
