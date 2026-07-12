namespace ReferWell.Domain.Entities;

public class MassCommMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }
    public MassCommCampaign? Campaign { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientType { get; set; } = "Patient";
    public Guid ReferralId { get; set; }
    public string ReferralCaseNo { get; set; } = string.Empty;
    public string RenderedSubject { get; set; } = string.Empty;
    public string RenderedBody { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class MassCommCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string FilterCriteria { get; set; } = string.Empty; // JSON filter
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Sending, Completed
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<MassCommMessage> Messages { get; set; } = new List<MassCommMessage>();
}
