using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReferWell.Domain.Entities;
using ReferWell.Infrastructure.Data;
using ReferWell.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly SecurityAuditService _audit;

    public AuthController(AppDbContext db, IConfiguration config, SecurityAuditService audit)
    {
        _db = db;
        _config = config;
        _audit = audit;
    }

    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string Token, string FullName, string Email, List<string> Roles, string? Title);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // OWASP: Parameterized query via LINQ (EF Core)
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await _audit.LogAsync("LoginFailed", actorEmail: request.Email, details: "Invalid credentials");
            // OWASP: Generic error message — don't reveal if email or password is wrong
            return Unauthorized(new { message = "Invalid credentials." });
        }

        user.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("LoginSucceeded", user.Id, user.Email);

        var token = GenerateJwtToken(user);
        return Ok(new LoginResponse(token, user.FullName, user.Email, user.UserRoles.Select(ur => ur.Role.ToString()).ToList(), user.Title));
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtConfig = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("fullName", user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var ur in user.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, ur.Role.ToString()));
        }

        var expireMinutes = int.Parse(jwtConfig["ExpireMinutes"] ?? "480");
        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
