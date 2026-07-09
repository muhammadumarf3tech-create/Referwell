using ReferWell.Domain.Enums;

namespace ReferWell.Domain.Exceptions;

public class InvalidReferralTransitionException : Exception
{
    public InvalidReferralTransitionException(ReferralStatus from, ReferralStatus to)
        : base($"Invalid referral transition from '{from}' to '{to}'.") { }
}

public class ReferralAlreadyClaimedException : Exception
{
    public ReferralAlreadyClaimedException(Guid referralId, Guid claimedByUserId)
        : base($"Referral '{referralId}' is already claimed by user '{claimedByUserId}'.") { }
}

public class ReferralNotFoundException : Exception
{
    public ReferralNotFoundException(Guid referralId)
        : base($"Referral '{referralId}' was not found.") { }
}
