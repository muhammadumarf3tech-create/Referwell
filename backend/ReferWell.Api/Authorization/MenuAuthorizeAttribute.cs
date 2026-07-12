using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ReferWell.Domain.Enums;
using ReferWell.Infrastructure.Data;
using System.Security.Claims;

namespace ReferWell.Api.Authorization;

/// <summary>
/// Authorizes access based on RoleMenuAccess configuration for the given menu item,
/// rather than hardcoded role names.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class MenuAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _menuItem;

    public MenuAuthorizeAttribute(string menuItem)
    {
        _menuItem = menuItem;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var roleNames = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var userRoles = new List<UserRole>();
        foreach (var name in roleNames)
        {
            if (Enum.TryParse<UserRole>(name, ignoreCase: true, out var role))
                userRoles.Add(role);
        }

        if (userRoles.Count == 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var hasAccess = await db.RoleMenuAccesses
            .AsNoTracking()
            .AnyAsync(m =>
                m.MenuItem == _menuItem
                && m.HasAccess
                && userRoles.Contains(m.Role));

        if (!hasAccess)
            context.Result = new ForbidResult();
    }
}
