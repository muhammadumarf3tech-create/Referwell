using ReferWell.Application.Common.Models;

namespace ReferWell.Application.Referrals;

public interface IReferralService
{
    Task<AppResult> GetNextCaseNoAsync(CancellationToken ct = default);
    Task<AppResult> GetReferralsAsync(GetReferralsQuery query, CancellationToken ct = default);
    Task<AppResult> GetReferralAsync(Guid id, CancellationToken ct = default);
    Task<AppResult> CreateReferralAsync(CreateReferralRequest request, CancellationToken ct = default);
    Task<AppResult> UpdateReferralAsync(Guid id, UpdateReferralRequest request, CancellationToken ct = default);
    Task<AppResult> ClaimReferralAsync(Guid id, ConcurrencyRequest req, CancellationToken ct = default);
    Task<AppResult> ReleaseReferralAsync(Guid id, CancellationToken ct = default);
    Task<AppResult> TransitionReferralAsync(Guid id, TransitionRequest req, CancellationToken ct = default);
    Task<AppResult> PauseSlaAsync(Guid id, PauseSlaRequest req, CancellationToken ct = default);
    Task<AppResult> ResumeSlaAsync(Guid id, ConcurrencyRequest req, CancellationToken ct = default);
    Task<AppResult> UploadAttachmentAsync(Guid id, string fileName, string contentType, long length, Stream content, CancellationToken ct = default);
    Task<AppResult> GetAttachmentAsync(Guid attachmentId, bool download, CancellationToken ct = default);
}
