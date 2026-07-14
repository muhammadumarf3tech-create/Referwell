using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ReferWell.Application.Common.Interfaces;
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
        if (roleNames.Count == 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        var checker = context.HttpContext.RequestServices.GetRequiredService<IMenuAccessChecker>();
        var hasAccess = await checker.HasMenuAccessAsync(roleNames, _menuItem);

        if (!hasAccess)
            context.Result = new ForbidResult();
    }
}
