using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Infrastructure.Data;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .Select(u => new
            {
                u.Id, u.FullName, u.Email,
                Role = u.Role.ToString(),
                u.IsActive, u.CreatedAt, u.LastLoginAt
            })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { message = "Email already in use." });

        var user = new ApplicationUser
        {
            FullName = req.FullName,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { user.Id, user.FullName, user.Email, Role = user.Role.ToString() });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.FullName = req.FullName;
        user.Role = req.Role;
        user.IsActive = req.IsActive;

        if (!string.IsNullOrWhiteSpace(req.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

        await _db.SaveChangesAsync();
        return Ok(new { message = "User updated." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "User deactivated." });
    }
}

public record CreateUserRequest(string FullName, string Email, string Password, UserRole Role);
public record UpdateUserRequest(string FullName, UserRole Role, bool IsActive, string? NewPassword);
