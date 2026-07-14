using ReferWell.Domain.Enums;

namespace ReferWell.Application.Users;

public record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    List<UserRole> Roles,
    string? Title,
    string? Gender,
    string? PhoneNumber);

public record UpdateUserRequest(
    string FullName,
    string Email,
    List<UserRole> Roles,
    bool IsActive,
    string? NewPassword,
    string? Title,
    string? Gender,
    string? PhoneNumber);

public record UserDto(
    Guid Id,
    string FullName,
    string? Email,
    List<string> Roles,
    bool IsActive,
    DateTime? CreatedAt,
    DateTime? LastLoginAt,
    string? Title,
    string? Gender,
    string? PhoneNumber);
