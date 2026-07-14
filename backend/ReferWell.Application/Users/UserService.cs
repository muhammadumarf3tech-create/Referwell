using Microsoft.EntityFrameworkCore;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Application.Common.Models;
using ReferWell.Domain.Entities;

namespace ReferWell.Application.Users;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecurityAuditLogger _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IMenuAccessChecker _menuAccess;

    public UserService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        ISecurityAuditLogger audit,
        ICurrentUser currentUser,
        IMenuAccessChecker menuAccess)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _audit = audit;
        _currentUser = currentUser;
        _menuAccess = menuAccess;
    }

    private Task<bool> HasUserManagementAccessAsync(CancellationToken ct) =>
        _menuAccess.HasMenuAccessAsync(_currentUser.Roles, "User Management", ct);

    public async Task<AppResult> GetUsersAsync(string? search, int? page, int? pageSize, CancellationToken ct = default)
    {
        var canManage = await HasUserManagementAccessAsync(ct);
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
            var totalCount = await query.CountAsync(ct);
            var raw = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNum - 1) * size)
                .Take(size)
                .ToListAsync(ct);

            var items = raw.Select(u => ProjectUser(u, canManage)).ToList();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            return AppResult.Success(new
            {
                items,
                totalCount,
                page = pageNum,
                pageSize = size,
                totalPages
            });
        }

        var all = await query.OrderByDescending(u => u.CreatedAt).ToListAsync(ct);
        return AppResult.Success(all.Select(u => ProjectUser(u, canManage)));
    }

    public async Task<AppResult> GetUserAsync(Guid id, CancellationToken ct = default)
    {
        var canManage = await HasUserManagementAccessAsync(ct);
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return AppResult.NotFound();
        return AppResult.Success(ProjectUser(user, canManage));
    }

    public async Task<AppResult> CreateAsync(CreateUserRequest req, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email, ct))
            return AppResult.BadRequest("Email already in use.");

        var user = new ApplicationUser
        {
            FullName = req.FullName,
            Email = req.Email,
            PasswordHash = _passwordHasher.Hash(req.Password),
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
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UserCreated", _currentUser.UserId, details: $"Created user {user.Email}", ct: ct);
        return AppResult.Created(ProjectUser(user, includeSensitive: true));
    }

    public async Task<AppResult> UpdateAsync(Guid id, UpdateUserRequest req, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return AppResult.NotFound();

        if (!string.Equals(user.Email, req.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _db.Users.AnyAsync(u => u.Email == req.Email && u.Id != id, ct))
                return AppResult.BadRequest("Email already in use.");
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
            await _db.UserRoles.AddRangeAsync(rolesToAdd, ct);
        }

        var passwordChanged = !string.IsNullOrWhiteSpace(req.NewPassword);
        if (passwordChanged)
        {
            user.PasswordHash = _passwordHasher.Hash(req.NewPassword!);
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(
            "UserUpdated",
            _currentUser.UserId,
            details: $"Updated user {user.Email}" + (passwordChanged ? " (password changed)" : ""),
            ct: ct);
        return AppResult.Success(new { message = "User updated." });
    }

    public async Task<AppResult> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([id], ct);
        if (user == null) return AppResult.NotFound();
        user.IsActive = false;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UserDeactivated", _currentUser.UserId, details: $"Deactivated user {user.Email}", ct: ct);
        return AppResult.Success(new { message = "User deactivated." });
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
