using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReferWell.Api.Authorization;
using ReferWell.Api.Extensions;
using ReferWell.Application.Config;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[MenuAuthorize("Priority Config")]
public class ConfigController : ControllerBase
{
    private readonly IConfigService _config;

    public ConfigController(IConfigService config) => _config = config;

    [HttpGet("weights")]
    public async Task<IActionResult> GetWeights(CancellationToken ct)
    {
        var result = await _config.GetWeightsAsync(ct);
        return result.ToActionResult(this);
    }

    [HttpPost("weights")]
    public async Task<IActionResult> UpdateWeights([FromBody] UpdateWeightsRequest request, CancellationToken ct)
    {
        var result = await _config.UpdateWeightsAsync(request, ct);
        return result.ToActionResult(this);
    }
}
