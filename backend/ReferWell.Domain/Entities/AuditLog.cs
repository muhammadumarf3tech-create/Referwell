using ReferWell.Domain.Enums;

namespace ReferWell.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReferralId { get; set; }
    public Referral? Referral { get; set; }
    public Guid PerformedByUserId { get; set; }
    public ApplicationUser? PerformedByUser { get; set; }
    public ReferralStatus? FromStatus { get; set; }
    public ReferralStatus? ToStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
