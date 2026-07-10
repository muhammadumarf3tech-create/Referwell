using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReferWell.Domain.Entities;
using ReferWell.Infrastructure.Data;

namespace ReferWell.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search)
    {
        var query = _db.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) 
                                  || p.NhiNumber.ToLower().Contains(search)
                                  || p.Email.ToLower().Contains(search));
        }

        var patients = await query.OrderBy(p => p.Name).ToListAsync();
        return Ok(patients);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest req)
    {
        if (await _db.Patients.AnyAsync(p => p.NhiNumber == req.NhiNumber))
            return BadRequest(new { message = "NHI Number already exists." });

        var patient = new Patient
        {
            Name = req.Name,
            DateOfBirth = req.DateOfBirth,
            Email = req.Email ?? string.Empty,
            PhoneNumber = req.PhoneNumber ?? string.Empty,
            NhiNumber = req.NhiNumber,
            Gender = req.Gender ?? string.Empty
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        return Ok(patient);
    }
}

public record CreatePatientRequest(
    string Name, 
    DateTime DateOfBirth, 
    string? Email, 
    string? PhoneNumber, 
    string NhiNumber,
    string? Gender);
