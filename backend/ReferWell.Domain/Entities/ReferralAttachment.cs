using System.Text.Json.Serialization;

namespace ReferWell.Domain.Entities;

public class ReferralAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReferralId { get; set; }
    
    [JsonIgnore]
    public Referral Referral { get; set; } = null!;
    
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.Now;
}
