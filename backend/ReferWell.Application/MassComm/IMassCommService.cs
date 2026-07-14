using ReferWell.Application.Common.Models;

namespace ReferWell.Application.MassComm;

public interface IMassCommService
{
    Task<AppResult> GetCampaignsAsync(
        string? search = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 15,
        CancellationToken ct = default);

    Task<AppResult> GetFilterOptionsAsync(CancellationToken ct = default);

    Task<AppResult> CreateCampaignAsync(CreateCampaignRequest req, CancellationToken ct = default);

    Task<AppResult> PreviewCampaignAsync(CreateCampaignRequest req, CancellationToken ct = default);

    Task<AppResult> GetMessagesAsync(Guid id, CancellationToken ct = default);
}
