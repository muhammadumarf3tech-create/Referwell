using ReferWell.Application.Common.Models;

namespace ReferWell.Application.Patients;

public interface IPatientService
{
    Task<AppResult> GetPatientsAsync(string? search, CancellationToken ct = default);
    Task<AppResult> CreateAsync(CreatePatientRequest req, CancellationToken ct = default);
}
