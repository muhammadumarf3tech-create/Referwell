namespace ReferWell.Application.Common.Interfaces;

public interface ISecurityAuditLogger
{
    Task LogAsync(
        string action,
        Guid? actorUserId = null,
        string? actorEmail = null,
        string? details = null,
        CancellationToken ct = default);
}
