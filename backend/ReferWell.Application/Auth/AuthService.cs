using Microsoft.EntityFrameworkCore;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Application.Common.Models;

namespace ReferWell.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ISecurityAuditLogger _audit;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwt,
        ISecurityAuditLogger audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _audit = audit;
    }

    public async Task<AppResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, ct);

        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await _audit.LogAsync("LoginFailed", actorEmail: request.Email, details: "Invalid credentials", ct: ct);
            return AppResult.Unauthorized("Invalid credentials.");
        }

        user.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("LoginSucceeded", user.Id, user.Email, ct: ct);

        var token = _jwt.GenerateToken(user);
        return AppResult.Success(new LoginResponse(
            token,
            user.FullName,
            user.Email,
            user.UserRoles.Select(ur => ur.Role.ToString()).ToList(),
            user.Title));
    }
}
