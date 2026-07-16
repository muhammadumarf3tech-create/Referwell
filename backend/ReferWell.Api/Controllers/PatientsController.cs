using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReferWell.Api.Authorization;
using ReferWell.Api.Extensions;
using ReferWell.Application.Patients;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patients;

    public PatientsController(IPatientService patients) => _patients = patients;

    [HttpGet]
    public async Task<IActionResult> GetPatients(
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var result = await _patients.GetPatientsAsync(search, page, pageSize, ct);
        return result.ToActionResult(this);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatient(Guid id, CancellationToken ct)
    {
        var result = await _patients.GetPatientAsync(id, ct);
        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest req, CancellationToken ct)
    {
        var result = await _patients.CreateAsync(req, ct);
        return result.ToActionResult(this);
    }

    [HttpPut("{id}")]
    [MenuAuthorize("Patients")]
    public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] UpdatePatientRequest req, CancellationToken ct)
    {
        var result = await _patients.UpdateAsync(id, req, ct);
        return result.ToActionResult(this);
    }
}
