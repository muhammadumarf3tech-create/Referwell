using ReferWell.Domain.Enums;

namespace ReferWell.Application.MenuAccess;

public record RoleMenuAccessDto(UserRole Role, string MenuItem, bool HasAccess);
