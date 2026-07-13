using ReferWell.Domain.Entities;
using ReferWell.Domain.Exceptions;

namespace ReferWell.Tests;

public class ReferralClaimTests
{
    [Fact]
    public void Claim_SetsUserAndPreventsOtherClaims()
    {
        var referral = new Referral();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        referral.Claim(user1);

        Assert.Equal(user1, referral.ClaimedByUserId);
        Assert.NotNull(referral.ClaimedAt);

        referral.Claim(user1);
        Assert.Throws<ReferralAlreadyClaimedException>(() => referral.Claim(user2));
    }

    [Fact]
    public void Release_ClearsClaim()
    {
        var referral = new Referral();
        var user = Guid.NewGuid();
        referral.Claim(user);

        referral.Release();

        Assert.Null(referral.ClaimedByUserId);
        Assert.Null(referral.ClaimedAt);
    }

    [Fact]
    public void Release_AllowsReclaimByAnotherUser()
    {
        var referral = new Referral();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        referral.Claim(user1);
        referral.Release();
        referral.Claim(user2);

        Assert.Equal(user2, referral.ClaimedByUserId);
    }
}
