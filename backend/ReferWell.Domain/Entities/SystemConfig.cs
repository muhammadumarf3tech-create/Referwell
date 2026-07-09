namespace ReferWell.Domain.Entities;

public class SystemConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Priority weight config keys:
    // "weight_urgency"  => e.g. "50"
    // "weight_waittime" => e.g. "30"
    // "weight_patient"  => e.g. "20"
}
