using ReferWell.Application.Common.Models;

namespace ReferWell.Application.ReferralImport;

public interface IReferralImportService
{
    Task<AppResult> GetBatchesAsync(
        string? search = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 15,
        CancellationToken ct = default);

    Task<AppResult> GetBatchAsync(Guid id, CancellationToken ct = default);

    Task<AppResult> GetBatchRowsAsync(
        Guid id,
        string? status = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    AppResult DownloadTemplate();

    Task<AppResult> ImportAsync(string fileName, long fileLength, Stream content, CancellationToken ct = default);
}
