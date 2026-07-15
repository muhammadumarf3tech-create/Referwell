using ReferWell.Domain.Enums;
using ReferWell.Domain.Exceptions;

namespace ReferWell.Domain.Entities;

public class Referral
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string CaseNo { get; set; } = string.Empty;

    /// <summary>True when the referral was created via CSV/legacy bulk import.</summary>
    public bool IsMigrated { get; set; } = false;

    public string ReferringGPId { get; set; } = string.Empty; // FK to ApplicationUser
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public string SpecialistType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public UrgencyLevel Urgency { get; set; }
    public ReferralStatus Status { get; set; } = ReferralStatus.Received;

    // SLA
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
    public DateTime SlaDeadline { get; set; }
    public bool SlaBreach { get; set; } = false;
    /// <summary>When true, the SLA clock is frozen (e.g. waiting on patient).</summary>
    public bool SlaPaused { get; set; } = false;
    public DateTime? SlaPausedAt { get; set; }
    public string? SlaPauseReason { get; set; }

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

    public DateTime CreatedAt { get; set; } = DateTime.Now;
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
        UpdatedAt = DateTime.Now;

        // Closed referrals drop any active pause without extending the deadline
        if ((newStatus == ReferralStatus.Declined || newStatus == ReferralStatus.Completed) && SlaPaused)
            ClearSlaPause();
    }

    /// <summary>
    /// Freezes the SLA clock (typically while waiting on the patient).
    /// Paused referrals are excluded from active breach filters/stats until resumed.
    /// </summary>
    public void PauseSla(string reason = "WaitingOnPatient", DateTime? asOf = null)
    {
        if (Status is ReferralStatus.Declined or ReferralStatus.Completed)
            throw new InvalidSlaPauseException("Cannot pause SLA on a closed referral.");

        if (SlaPaused)
            throw new InvalidSlaPauseException("SLA is already paused.");

        var reasonText = string.IsNullOrWhiteSpace(reason) ? "WaitingOnPatient" : reason.Trim();
        if (reasonText.Length > 100)
            reasonText = reasonText[..100];

        SlaPaused = true;
        SlaPausedAt = asOf ?? DateTime.Now;
        SlaPauseReason = reasonText;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>True when the referral has an SLA breach that is currently actionable (not paused).</summary>
    public bool IsActivelySlaBreached => SlaBreach && !SlaPaused;

    /// <summary>
    /// Resumes the SLA clock and extends the deadline by the paused duration.
    /// </summary>
    public void ResumeSla(DateTime? asOf = null)
    {
        if (!SlaPaused || !SlaPausedAt.HasValue)
            throw new InvalidSlaPauseException("SLA is not paused.");

        var now = asOf ?? DateTime.Now;
        var pausedFor = now - SlaPausedAt.Value;
        if (pausedFor > TimeSpan.Zero)
            SlaDeadline = SlaDeadline.Add(pausedFor);

        ClearSlaPause();
        UpdatedAt = DateTime.Now;
        EvaluateSlaBreach(now);
    }

    private void ClearSlaPause()
    {
        SlaPaused = false;
        SlaPausedAt = null;
        SlaPauseReason = null;
    }

    public void Claim(Guid userId)
    {
        if (ClaimedByUserId.HasValue && ClaimedByUserId != userId)
            throw new ReferralAlreadyClaimedException(Id, ClaimedByUserId.Value);

        ClaimedByUserId = userId;
        ClaimedAt = DateTime.Now;
        // Claiming assigns hospital-side ownership for the shared triage queue.
        AssignedToUserId = userId;
        UpdatedAt = DateTime.Now;
    }

    public void Release()
    {
        ClaimedByUserId = null;
        ClaimedAt = null;
        UpdatedAt = DateTime.Now;
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

    /// <summary>
    /// Time-to-first-triage SLA: marks breach when still <see cref="ReferralStatus.Received"/>
    /// (no triage action yet) and the deadline has passed. Once triage has occurred the flag
    /// is left sticky for historical reporting. Returns true when newly breached.
    /// </summary>
    public bool EvaluateSlaBreach(DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.Now;

        // Clock frozen — do not mark or clear breach while paused
        if (SlaPaused)
            return false;

        // Past first action — keep any existing breach flag as history
        if (Status != ReferralStatus.Received)
            return false;

        if (now > SlaDeadline)
        {
            if (SlaBreach) return false;
            SlaBreach = true;
            return true;
        }

        // Still inside the window (e.g. urgency extended the deadline)
        SlaBreach = false;
        return false;
    }
}
