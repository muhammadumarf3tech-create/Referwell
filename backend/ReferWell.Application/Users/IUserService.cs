using ReferWell.Application.Common.Models;

namespace ReferWell.Application.Users;

public interface IUserService
{
    Task<AppResult> GetUsersAsync(string? search, int? page, int? pageSize, CancellationToken ct = default);
    Task<AppResult> GetUserAsync(Guid id, CancellationToken ct = default);
    Task<AppResult> CreateAsync(CreateUserRequest req, CancellationToken ct = default);
    Task<AppResult> UpdateAsync(Guid id, UpdateUserRequest req, CancellationToken ct = default);
    Task<AppResult> DeactivateAsync(Guid id, CancellationToken ct = default);
}
