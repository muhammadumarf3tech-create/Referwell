using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReferWell.Api.Extensions;
using ReferWell.Application.Common.Models;
using ReferWell.Application.Referrals;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferralsController : ControllerBase
{
    private const long MaxPdfBytes = 20L * 1024 * 1024; // 20 MB

    private readonly IReferralService _referrals;

    public ReferralsController(IReferralService referrals) => _referrals = referrals;

    [HttpGet("next-case-no")]
    public async Task<IActionResult> GetNextCaseNo(CancellationToken ct)
    {
        var result = await _referrals.GetNextCaseNoAsync(ct);
        return result.ToActionResult(this);
    }

    [HttpGet]
    public async Task<IActionResult> GetReferrals(
        [FromQuery] string? status,
        [FromQuery] string? urgency,
        [FromQuery] string? assignedTo,
        [FromQuery] string? patientSearch,
        [FromQuery] string? caseNo,
        [FromQuery] string? sortBy,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool? slaBreach,
        [FromQuery] bool? isMigrated,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken ct = default)
    {
        var result = await _referrals.GetReferralsAsync(new GetReferralsQuery(
            status, urgency, assignedTo, patientSearch, caseNo, sortBy,
            fromDate, toDate, slaBreach, isMigrated, page, pageSize), ct);
        return result.ToActionResult(this);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReferral(Guid id, CancellationToken ct)
    {
        var result = await _referrals.GetReferralAsync(id, ct);
        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReferral([FromBody] CreateReferralRequest request, CancellationToken ct)
    {
        var result = await _referrals.CreateReferralAsync(request, ct);
        if (result.Status == AppStatus.Created && TryGetId(result.Value, out var id))
            return CreatedAtAction(nameof(GetReferral), new { id }, result.Value);
        return result.ToActionResult(this);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReferral(Guid id, [FromBody] UpdateReferralRequest request, CancellationToken ct)
    {
        var result = await _referrals.UpdateReferralAsync(id, request, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/claim")]
    [Authorize(Roles = "TriageNurse,Admin,GP")]
    public async Task<IActionResult> ClaimReferral(Guid id, [FromBody] ConcurrencyRequest req, CancellationToken ct)
    {
        var result = await _referrals.ClaimReferralAsync(id, req, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/release")]
    [Authorize(Roles = "TriageNurse,Admin,GP")]
    public async Task<IActionResult> ReleaseReferral(Guid id, CancellationToken ct)
    {
        var result = await _referrals.ReleaseReferralAsync(id, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/transition")]
    [Authorize(Roles = "TriageNurse,Admin,GP")]
    public async Task<IActionResult> TransitionReferral(Guid id, [FromBody] TransitionRequest req, CancellationToken ct)
    {
        var result = await _referrals.TransitionReferralAsync(id, req, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/pause-sla")]
    [Authorize(Roles = "TriageNurse,Admin,GP")]
    public async Task<IActionResult> PauseSla(Guid id, [FromBody] PauseSlaRequest req, CancellationToken ct)
    {
        var result = await _referrals.PauseSlaAsync(id, req, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/resume-sla")]
    [Authorize(Roles = "TriageNurse,Admin,GP")]
    public async Task<IActionResult> ResumeSla(Guid id, [FromBody] ConcurrencyRequest req, CancellationToken ct)
    {
        var result = await _referrals.ResumeSlaAsync(id, req, ct);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/attachments")]
    [RequestSizeLimit(MaxPdfBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxPdfBytes)]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile? file, CancellationToken ct)
    {
        if (file is null)
            return BadRequest(new { message = "No file uploaded." });

        await using var stream = file.OpenReadStream();
        var result = await _referrals.UploadAttachmentAsync(id, file.FileName, file.ContentType, file.Length, stream, ct);
        return result.ToActionResult(this);
    }

    [HttpGet("attachments/{attachmentId}")]
    public async Task<IActionResult> GetAttachment(Guid attachmentId, [FromQuery] bool download = false, CancellationToken ct = default)
    {
        var result = await _referrals.GetAttachmentAsync(attachmentId, download, ct);
        return result.ToActionResult(this);
    }

    private static bool TryGetId(object? value, out Guid id)
    {
        id = default;
        if (value is null) return false;

        var prop = value.GetType().GetProperty("Id");
        if (prop?.GetValue(value) is Guid guid)
        {
            id = guid;
            return true;
        }

        return false;
    }
}
