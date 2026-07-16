using Microsoft.EntityFrameworkCore;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Application.Common.Models;
using ReferWell.Domain.Entities;

namespace ReferWell.Application.Patients;

public class PatientService : IPatientService
{
    private readonly IApplicationDbContext _db;

    public PatientService(IApplicationDbContext db) => _db = db;

    public async Task<AppResult> GetPatientsAsync(string? search, int? page, int? pageSize, CancellationToken ct = default)
    {
        var query = _db.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim();
            query = query.Where(p => p.Name.Contains(cleanSearch)
                                  || p.NhiNumber.Contains(cleanSearch)
                                  || p.Email.Contains(cleanSearch)
                                  || p.PhoneNumber.Contains(cleanSearch));
        }

        var size = Math.Clamp(pageSize ?? 15, 1, 100);

        if (page.HasValue)
        {
            var pageNum = Math.Max(1, page.Value);
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((pageNum - 1) * size)
                .Take(size)
                .ToListAsync(ct);

            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            return AppResult.Success(new
            {
                items,
                totalCount,
                page = pageNum,
                pageSize = size,
                totalPages
            });
        }

        // Unpaginated list for referral patient picker
        var all = await query.OrderBy(p => p.Name).ToListAsync(ct);
        return AppResult.Success(all);
    }

    public async Task<AppResult> GetPatientAsync(Guid id, CancellationToken ct = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (patient == null) return AppResult.NotFound();
        return AppResult.Success(patient);
    }

    public async Task<AppResult> CreateAsync(CreatePatientRequest req, CancellationToken ct = default)
    {
        if (await _db.Patients.AnyAsync(p => p.NhiNumber == req.NhiNumber, ct))
            return AppResult.BadRequest("NHI Number already exists.");

        var patient = new Patient
        {
            Name = req.Name.Trim(),
            DateOfBirth = req.DateOfBirth,
            Email = req.Email.Trim(),
            PhoneNumber = req.PhoneNumber ?? string.Empty,
            NhiNumber = req.NhiNumber.Trim().ToUpperInvariant(),
            Gender = req.Gender ?? string.Empty
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(ct);

        return AppResult.Success(patient);
    }

    public async Task<AppResult> UpdateAsync(Guid id, UpdatePatientRequest req, CancellationToken ct = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (patient == null) return AppResult.NotFound();

        var nhi = req.NhiNumber.Trim().ToUpperInvariant();
        if (await _db.Patients.AnyAsync(p => p.NhiNumber == nhi && p.Id != id, ct))
            return AppResult.BadRequest("NHI Number already exists.");

        patient.Name = req.Name.Trim();
        patient.DateOfBirth = req.DateOfBirth;
        patient.Email = req.Email.Trim();
        patient.PhoneNumber = req.PhoneNumber ?? string.Empty;
        patient.NhiNumber = nhi;
        patient.Gender = req.Gender ?? string.Empty;

        await _db.SaveChangesAsync(ct);
        return AppResult.Success(patient);
    }
}
