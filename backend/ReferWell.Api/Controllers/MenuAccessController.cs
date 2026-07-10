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
public class MenuAccessController : ControllerBase
{
    private readonly AppDbContext _db;

    public MenuAccessController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetMenuAccess()
    {
        var accesses = await _db.RoleMenuAccesses.ToListAsync();
        return Ok(accesses);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMenuAccess([FromBody] List<RoleMenuAccessDto> req)
    {
        var dbAccesses = await _db.RoleMenuAccesses.ToListAsync();

        foreach (var r in req)
        {
            var match = dbAccesses.FirstOrDefault(x => x.Role == r.Role && x.MenuItem == r.MenuItem);
            if (match != null)
            {
                match.HasAccess = r.HasAccess;
            }
            else
            {
                _db.RoleMenuAccesses.Add(new RoleMenuAccess
                {
                    Role = r.Role,
                    MenuItem = r.MenuItem,
                    HasAccess = r.HasAccess
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Menu access configuration updated." });
    }
}

public record RoleMenuAccessDto(UserRole Role, string MenuItem, bool HasAccess);
