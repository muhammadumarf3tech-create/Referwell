using Microsoft.AspNetCore.Mvc;
using ReferWell.Api.Extensions;
using ReferWell.Application.Auth;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        return result.ToActionResult(this);
    }
}
