using ReferWell.Application.Common.Models;

namespace ReferWell.Application.Auth;

public interface IAuthService
{
    Task<AppResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
