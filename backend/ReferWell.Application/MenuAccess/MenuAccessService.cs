using Microsoft.EntityFrameworkCore;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Application.Common.Models;
using ReferWell.Domain.Entities;

namespace ReferWell.Application.MenuAccess;

public class MenuAccessService : IMenuAccessService
{
    private readonly IApplicationDbContext _db;
    private readonly ISecurityAuditLogger _audit;
    private readonly ICurrentUser _currentUser;

    public MenuAccessService(
        IApplicationDbContext db,
        ISecurityAuditLogger audit,
        ICurrentUser currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<AppResult> GetAsync(CancellationToken ct = default)
    {
        var accesses = await _db.RoleMenuAccesses.ToListAsync(ct);
        return AppResult.Success(accesses);
    }

    public async Task<AppResult> UpdateAsync(List<RoleMenuAccessDto> req, CancellationToken ct = default)
    {
        var dbAccesses = await _db.RoleMenuAccesses.ToListAsync(ct);

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

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("MenuAccessUpdated", _currentUser.UserId, details: $"Updated {req.Count} menu access rows", ct: ct);
        return AppResult.Success(new { message = "Menu access configuration updated." });
    }
}
