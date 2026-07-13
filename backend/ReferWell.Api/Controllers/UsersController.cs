using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReferWell.Api.Authorization;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Infrastructure.Data;
using ReferWell.Infrastructure.Services;
using System.Security.Claims;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SecurityAuditService _audit;

    public UsersController(AppDbContext db, SecurityAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

    private async Task<bool> HasUserManagementAccessAsync()
    {
        var roleNames = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var roles = new List<UserRole>();
        foreach (var name in roleNames)
        {
            if (Enum.TryParse<UserRole>(name, ignoreCase: true, out var role))
                roles.Add(role);
        }

        return await _db.RoleMenuAccesses.AsNoTracking()
            .AnyAsync(m => m.MenuItem == "User Management" && m.HasAccess && roles.Contains(m.Role));
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var canManage = await HasUserManagementAccessAsync();
        var query = _db.Users.Include(u => u.UserRoles).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var cleanSearch = search.Trim();
            query = query.Where(u => u.FullName.Contains(cleanSearch) || u.Email.Contains(cleanSearch));
        }

        var size = Math.Clamp(pageSize ?? 15, 1, 100);

        if (page.HasValue)
        {
            var pageNum = Math.Max(1, page.Value);
            var totalCount = await query.CountAsync();
            var raw = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNum - 1) * size)
                .Take(size)
                .ToListAsync();

            var items = raw.Select(u => ProjectUser(u, canManage)).ToList();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            return Ok(new
            {
                items,
                totalCount,
                page = pageNum,
                pageSize = size,
                totalPages
            });
        }

        var all = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        return Ok(all.Select(u => ProjectUser(u, canManage)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var canManage = await HasUserManagementAccessAsync();
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return Ok(ProjectUser(user, canManage));
    }

    [HttpPost]
    [MenuAuthorize("User Management")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { message = "Email already in use." });

        var user = new ApplicationUser
        {
            FullName = req.FullName,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Title = req.Title,
            Gender = req.Gender ?? string.Empty,
            PhoneNumber = req.PhoneNumber ?? string.Empty
        };

        if (req.Roles != null)
        {
            foreach (var role in req.Roles)
            {
                user.UserRoles.Add(new ApplicationUserRole { Role = role });
            }
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("UserCreated", CurrentUserId, details: $"Created user {user.Email}");
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ProjectUser(user, includeSensitive: true));
    }

    [HttpPut("{id}")]
    [MenuAuthorize("User Management")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest req)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        if (!string.Equals(user.Email, req.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _db.Users.AnyAsync(u => u.Email == req.Email && u.Id != id))
            {
                return BadRequest(new { message = "Email already in use." });
            }
            user.Email = req.Email;
        }

        user.FullName = req.FullName;
        user.IsActive = req.IsActive;
        user.Title = req.Title;
        user.Gender = req.Gender ?? string.Empty;
        user.PhoneNumber = req.PhoneNumber ?? string.Empty;

        if (req.Roles != null)
        {
            var requestedRoles = req.Roles.Distinct().ToHashSet();
            var rolesToRemove = user.UserRoles
                .Where(ur => !requestedRoles.Contains(ur.Role))
                .ToList();
            var existingRoles = user.UserRoles
                .Select(ur => ur.Role)
                .ToHashSet();

            _db.UserRoles.RemoveRange(rolesToRemove);

            var rolesToAdd = requestedRoles
                .Where(role => !existingRoles.Contains(role))
                .Select(role => new ApplicationUserRole
                {
                    UserId = user.Id,
                    Role = role
                });
            await _db.UserRoles.AddRangeAsync(rolesToAdd);
        }

        var passwordChanged = !string.IsNullOrWhiteSpace(req.NewPassword);
        if (passwordChanged)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(
            "UserUpdated",
            CurrentUserId,
            details: $"Updated user {user.Email}" + (passwordChanged ? " (password changed)" : ""));
        return Ok(new { message = "User updated." });
    }

    [HttpDelete("{id}")]
    [MenuAuthorize("User Management")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = false;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("UserDeactivated", CurrentUserId, details: $"Deactivated user {user.Email}");
        return Ok(new { message = "User deactivated." });
    }

    private static object ProjectUser(ApplicationUser user, bool includeSensitive)
    {
        if (includeSensitive)
        {
            return new
            {
                user.Id,
                user.FullName,
                user.Email,
                Roles = user.UserRoles.Select(ur => ur.Role.ToString()).ToList(),
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                user.Title,
                user.Gender,
                user.PhoneNumber
            };
        }

        // Assignees / dropdowns — no PII beyond display name
        return new
        {
            user.Id,
            user.FullName,
            Roles = user.UserRoles.Select(ur => ur.Role.ToString()).ToList(),
            user.IsActive,
            user.Title
        };
    }
}

public record CreateUserRequest(string FullName, string Email, string Password, List<UserRole> Roles, string? Title, string? Gender, string? PhoneNumber);
public record UpdateUserRequest(string FullName, string Email, List<UserRole> Roles, bool IsActive, string? NewPassword, string? Title, string? Gender, string? PhoneNumber);
