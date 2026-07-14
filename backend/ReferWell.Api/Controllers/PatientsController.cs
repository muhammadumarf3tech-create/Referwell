using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> GetPatients([FromQuery] string? search, CancellationToken ct)
    {
        var result = await _patients.GetPatientsAsync(search, ct);
        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest req, CancellationToken ct)
    {
        var result = await _patients.CreateAsync(req, ct);
        return result.ToActionResult(this);
    }
}
