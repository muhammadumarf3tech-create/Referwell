using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReferWell.Api.Authorization;
using ReferWell.Api.Extensions;
using ReferWell.Application.Common.Models;
using ReferWell.Application.Users;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var result = await _users.GetUsersAsync(search, page, pageSize, ct);
        return result.ToActionResult(this);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var result = await _users.GetUserAsync(id, ct);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [MenuAuthorize("User Management")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        var result = await _users.CreateAsync(req, ct);
        if (result.Status == AppStatus.Created && TryGetId(result.Value, out var id))
            return CreatedAtAction(nameof(GetUser), new { id }, result.Value);
        return result.ToActionResult(this);
    }

    [HttpPut("{id}")]
    [MenuAuthorize("User Management")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var result = await _users.UpdateAsync(id, req, ct);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id}")]
    [MenuAuthorize("User Management")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken ct)
    {
        var result = await _users.DeactivateAsync(id, ct);
        return result.ToActionResult(this);
    }

    private static bool TryGetId(object? value, out Guid id)
    {
        id = default;
        if (value is null) return false;

        // Anonymous projected user exposes Id via reflection / JSON shape
        var prop = value.GetType().GetProperty("Id");
        if (prop?.GetValue(value) is Guid guid)
        {
            id = guid;
            return true;
        }

        return false;
    }
}
