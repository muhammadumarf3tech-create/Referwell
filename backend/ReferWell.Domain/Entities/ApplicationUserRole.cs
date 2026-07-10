using ReferWell.Domain.Enums;
using System.Text.Json.Serialization;

namespace ReferWell.Domain.Entities;

public class ApplicationUserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    
    [JsonIgnore]
    public ApplicationUser User { get; set; } = null!;
    
    public UserRole Role { get; set; }
}
