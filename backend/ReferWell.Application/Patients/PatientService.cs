using Microsoft.EntityFrameworkCore;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Application.Common.Models;
using ReferWell.Domain.Entities;

namespace ReferWell.Application.Patients;

public class PatientService : IPatientService
{
    private readonly IApplicationDbContext _db;

    public PatientService(IApplicationDbContext db) => _db = db;

    public async Task<AppResult> GetPatientsAsync(string? search, CancellationToken ct = default)
    {
        var query = _db.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search)
                                  || p.NhiNumber.ToLower().Contains(search)
                                  || p.Email.ToLower().Contains(search));
        }

        var patients = await query.OrderBy(p => p.Name).ToListAsync(ct);
        return AppResult.Success(patients);
    }

    public async Task<AppResult> CreateAsync(CreatePatientRequest req, CancellationToken ct = default)
    {
        if (await _db.Patients.AnyAsync(p => p.NhiNumber == req.NhiNumber, ct))
            return AppResult.BadRequest("NHI Number already exists.");

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
        await _db.SaveChangesAsync(ct);

        return AppResult.Success(patient);
    }
}
