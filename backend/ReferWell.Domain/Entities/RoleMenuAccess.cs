using ReferWell.Domain.Enums;

namespace ReferWell.Domain.Entities;

public class RoleMenuAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public UserRole Role { get; set; }
    public string MenuItem { get; set; } = string.Empty;
    public bool HasAccess { get; set; } = true;
}
