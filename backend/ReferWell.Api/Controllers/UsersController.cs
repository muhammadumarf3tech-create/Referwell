using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Infrastructure.Data;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var query = _db.Users.Include(u => u.UserRoles).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var cleanSearch = search.Trim();
            query = query.Where(u => u.FullName.Contains(cleanSearch) || u.Email.Contains(cleanSearch));
        }

        if (page.HasValue)
        {
            var size = pageSize ?? 15;
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page.Value - 1) * size)
                .Take(size)
                .Select(u => new
                {
                    u.Id, u.FullName, u.Email,
                    Roles = u.UserRoles.Select(ur => ur.Role.ToString()).ToList(),
                    u.IsActive, u.CreatedAt, u.LastLoginAt,
                    u.Password, u.Title, u.Gender, u.PhoneNumber
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            return Ok(new
            {
                items,
                totalCount,
                page = page.Value,
                pageSize = size,
                totalPages
            });
        }
        else
        {
            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id, u.FullName, u.Email,
                    Roles = u.UserRoles.Select(ur => ur.Role.ToString()).ToList(),
                    u.IsActive, u.CreatedAt, u.LastLoginAt,
                    u.Password, u.Title, u.Gender, u.PhoneNumber
                })
                .ToListAsync();
            return Ok(items);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return Ok(new
        {
            user.Id, user.FullName, user.Email,
            Roles = user.UserRoles.Select(ur => ur.Role.ToString()).ToList(),
            user.IsActive, user.CreatedAt, user.LastLoginAt,
            user.Password, user.Title, user.Gender, user.PhoneNumber
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { message = "Email already in use." });

        var user = new ApplicationUser
        {
            FullName = req.FullName,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Password = req.Password,
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
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { user.Id, user.FullName, user.Email, Roles = user.UserRoles.Select(ur => ur.Role.ToString()).ToList(), user.Password, user.Title, user.Gender, user.PhoneNumber });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
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
            // Delete all existing roles for this user directly via SQL to avoid EF concurrency tracking issues
            await _db.Database.ExecuteSqlRawAsync(
                "DELETE FROM tblUserRoles WHERE UserId = {0}", id);

            // Clear the navigation collection to reflect the database state
            user.UserRoles.Clear();

            // Add the new set of roles
            foreach (var role in req.Roles)
            {
                user.UserRoles.Add(new ApplicationUserRole { UserId = id, Role = role });
            }
        }

        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            user.Password = req.NewPassword;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "User updated." });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "User deactivated." });
    }
}

public record CreateUserRequest(string FullName, string Email, string Password, List<UserRole> Roles, string? Title, string? Gender, string? PhoneNumber);
public record UpdateUserRequest(string FullName, string Email, List<UserRole> Roles, bool IsActive, string? NewPassword, string? Title, string? Gender, string? PhoneNumber);
