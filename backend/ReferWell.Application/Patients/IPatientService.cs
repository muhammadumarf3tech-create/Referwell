using ReferWell.Application.Common.Models;

namespace ReferWell.Application.Patients;

public interface IPatientService
{
    Task<AppResult> GetPatientsAsync(string? search, int? page, int? pageSize, CancellationToken ct = default);
    Task<AppResult> GetPatientAsync(Guid id, CancellationToken ct = default);
    Task<AppResult> CreateAsync(CreatePatientRequest req, CancellationToken ct = default);
    Task<AppResult> UpdateAsync(Guid id, UpdatePatientRequest req, CancellationToken ct = default);
}
