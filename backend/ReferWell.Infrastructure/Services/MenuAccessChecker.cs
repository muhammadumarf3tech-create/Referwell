using Microsoft.EntityFrameworkCore;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Domain.Enums;
using ReferWell.Infrastructure.Data;

namespace ReferWell.Infrastructure.Services;

public class MenuAccessChecker : IMenuAccessChecker
{
    private readonly AppDbContext _db;

    public MenuAccessChecker(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasMenuAccessAsync(
        IEnumerable<string> roleNames,
        string menuItem,
        CancellationToken ct = default)
    {
        var userRoles = new List<UserRole>();
        foreach (var name in roleNames)
        {
            if (Enum.TryParse<UserRole>(name, ignoreCase: true, out var role))
                userRoles.Add(role);
        }

        if (userRoles.Count == 0)
            return false;

        return await _db.RoleMenuAccesses
            .AsNoTracking()
            .AnyAsync(m =>
                m.MenuItem == menuItem
                && m.HasAccess
                && userRoles.Contains(m.Role), ct);
    }
}
