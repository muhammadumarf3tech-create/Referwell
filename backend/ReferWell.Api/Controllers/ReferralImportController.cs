using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReferWell.Api.Authorization;
using ReferWell.Api.Extensions;
using ReferWell.Application.ReferralImport;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[MenuAuthorize("Referral Import")]
public class ReferralImportController : ControllerBase
{
    private readonly IReferralImportService _import;

    public ReferralImportController(IReferralImportService import) => _import = import;

    [HttpGet]
    public async Task<IActionResult> GetBatches(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken ct = default)
    {
        var result = await _import.GetBatchesAsync(search, status, fromDate, toDate, page, pageSize, ct);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBatch(Guid id, CancellationToken ct)
    {
        var result = await _import.GetBatchAsync(id, ct);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:guid}/rows")]
    public async Task<IActionResult> GetBatchRows(
        Guid id,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _import.GetBatchRowsAsync(id, status, search, page, pageSize, ct);
        return result.ToActionResult(this);
    }

    [HttpGet("template")]
    public IActionResult DownloadTemplate()
    {
        var result = _import.DownloadTemplate();
        return result.ToActionResult(this);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Please upload a CSV file." });

        await using var stream = file.OpenReadStream();
        var result = await _import.ImportAsync(file.FileName, file.Length, stream, ct);
        return result.ToActionResult(this);
    }
}
