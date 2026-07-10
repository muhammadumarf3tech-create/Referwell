using ReferWell.Domain.Enums;
using ReferWell.Domain.Exceptions;

namespace ReferWell.Domain.Entities;

public class Referral
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string CaseNo { get; set; } = string.Empty;

    public string ReferringGPId { get; set; } = string.Empty; // FK to ApplicationUser
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public string SpecialistType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public UrgencyLevel Urgency { get; set; }
    public ReferralStatus Status { get; set; } = ReferralStatus.Received;

    // SLA
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime SlaDeadline { get; set; }
    public bool SlaBreach { get; set; } = false;

    // Assignment management
    public Guid? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    // Claim management (multi-user concurrency)
    public Guid? ClaimedByUserId { get; set; }
    public ApplicationUser? ClaimedByUser { get; set; }
    public DateTime? ClaimedAt { get; set; }

    // Priority Score (calculated dynamically)
    public double PriorityScore { get; set; }

    // Optimistic Concurrency Token (MS SQL rowversion)
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<ReferralAttachment> Attachments { get; set; } = new List<ReferralAttachment>();

    // ─── State Machine ────────────────────────────────────────────────────────

    public static readonly Dictionary<ReferralStatus, ReferralStatus[]> AllowedTransitions = new()
    {
        [ReferralStatus.Received]  = [ReferralStatus.Triaged],
        [ReferralStatus.Triaged]   = [ReferralStatus.Accepted, ReferralStatus.Declined],
        [ReferralStatus.Accepted]  = [ReferralStatus.Booked],
        [ReferralStatus.Declined]  = [],
        [ReferralStatus.Booked]    = [ReferralStatus.Completed],
        [ReferralStatus.Completed] = []
    };

    public void TransitionTo(ReferralStatus newStatus)
    {
        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new InvalidReferralTransitionException(Status, newStatus);

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Claim(Guid userId)
    {
        if (ClaimedByUserId.HasValue && ClaimedByUserId != userId)
            throw new ReferralAlreadyClaimedException(Id, ClaimedByUserId.Value);

        ClaimedByUserId = userId;
        ClaimedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release()
    {
        ClaimedByUserId = null;
        ClaimedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    // ─── SLA Calculation ─────────────────────────────────────────────────────

    public static DateTime CalculateSlaDeadline(UrgencyLevel urgency, DateTime receivedAt) =>
        urgency switch
        {
            UrgencyLevel.Urgent     => receivedAt.AddHours(24),
            UrgencyLevel.SemiUrgent => receivedAt.AddDays(7),
            UrgencyLevel.Routine    => receivedAt.AddDays(30),
            _                       => receivedAt.AddDays(30)
        };
}
