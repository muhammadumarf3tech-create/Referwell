using ReferWell.Domain.Enums;

namespace ReferWell.Application.Referrals;

public record CreateReferralRequest(Guid PatientId, string SpecialistType, string Reason, UrgencyLevel Urgency, Guid? AssignedToUserId);

public record UpdateReferralRequest(string SpecialistType, string Reason, UrgencyLevel Urgency, Guid? AssignedToUserId, byte[]? RowVersion);

public record ConcurrencyRequest(byte[] RowVersion);

public record TransitionRequest(ReferralStatus NewStatus, byte[] RowVersion, string? Notes);

public record PauseSlaRequest(byte[] RowVersion, string? Reason = "WaitingOnPatient");

public record GetReferralsQuery(
    string? Status,
    string? Urgency,
    string? AssignedTo,
    string? PatientSearch,
    string? CaseNo,
    string? SortBy,
    DateTime? FromDate,
    DateTime? ToDate,
    bool? SlaBreach,
    bool? IsMigrated,
    int Page = 1,
    int PageSize = 15);
