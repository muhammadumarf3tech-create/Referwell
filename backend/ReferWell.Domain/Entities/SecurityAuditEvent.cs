namespace ReferWell.Domain.Entities;

/// <summary>
/// Non-referral security/admin audit events (auth, config, user admin, etc.).
/// </summary>
public class SecurityAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public ApplicationUser? ActorUser { get; set; }
    public string? ActorEmail { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}
