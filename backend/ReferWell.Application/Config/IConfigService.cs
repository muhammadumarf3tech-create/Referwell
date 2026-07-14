using ReferWell.Application.Common.Models;

namespace ReferWell.Application.Config;

public interface IConfigService
{
    Task<AppResult> GetWeightsAsync(CancellationToken ct = default);
    Task<AppResult> UpdateWeightsAsync(UpdateWeightsRequest request, CancellationToken ct = default);
}
