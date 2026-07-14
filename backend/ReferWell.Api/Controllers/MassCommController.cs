using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReferWell.Api.Authorization;
using ReferWell.Api.Extensions;
using ReferWell.Application.MassComm;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[MenuAuthorize("Mass Communications")]
public class MassCommController : ControllerBase
{
    private readonly IMassCommService _massComm;

    public MassCommController(IMassCommService massComm) => _massComm = massComm;

    [HttpGet]
    public async Task<IActionResult> GetCampaigns(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken ct = default)
    {
        var result = await _massComm.GetCampaignsAsync(search, status, fromDate, toDate, page, pageSize, ct);
        return result.ToActionResult(this);
    }

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken ct)
    {
        var result = await _massComm.GetFilterOptionsAsync(ct);
        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest req, CancellationToken ct)
    {
        var result = await _massComm.CreateCampaignAsync(req, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("preview")]
    public async Task<IActionResult> PreviewCampaign([FromBody] CreateCampaignRequest req, CancellationToken ct)
    {
        var result = await _massComm.PreviewCampaignAsync(req, ct);
        return result.ToActionResult(this);
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct)
    {
        var result = await _massComm.GetMessagesAsync(id, ct);
        return result.ToActionResult(this);
    }
}
