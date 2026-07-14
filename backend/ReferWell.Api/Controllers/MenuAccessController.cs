using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReferWell.Api.Authorization;
using ReferWell.Api.Extensions;
using ReferWell.Application.MenuAccess;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenuAccessController : ControllerBase
{
    private readonly IMenuAccessService _menuAccess;

    public MenuAccessController(IMenuAccessService menuAccess) => _menuAccess = menuAccess;

    [HttpGet]
    public async Task<IActionResult> GetMenuAccess(CancellationToken ct)
    {
        var result = await _menuAccess.GetAsync(ct);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [MenuAuthorize("Menu Access")]
    public async Task<IActionResult> UpdateMenuAccess([FromBody] List<RoleMenuAccessDto> req, CancellationToken ct)
    {
        var result = await _menuAccess.UpdateAsync(req, ct);
        return result.ToActionResult(this);
    }
}
