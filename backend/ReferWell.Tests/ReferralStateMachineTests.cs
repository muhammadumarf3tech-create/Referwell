using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Domain.Exceptions;

namespace ReferWell.Tests;

public class ReferralStateMachineTests
{
    [Theory]
    [InlineData(ReferralStatus.Received, ReferralStatus.Triaged, true)]
    [InlineData(ReferralStatus.Received, ReferralStatus.Accepted, false)]
    [InlineData(ReferralStatus.Triaged, ReferralStatus.Accepted, true)]
    [InlineData(ReferralStatus.Triaged, ReferralStatus.Declined, true)]
    [InlineData(ReferralStatus.Accepted, ReferralStatus.Booked, true)]
    [InlineData(ReferralStatus.Booked, ReferralStatus.Completed, true)]
    [InlineData(ReferralStatus.Declined, ReferralStatus.Received, false)]
    [InlineData(ReferralStatus.Completed, ReferralStatus.Booked, false)]
    [InlineData(ReferralStatus.Accepted, ReferralStatus.Declined, false)]
    public void TransitionTo_EnforcesAllowedTransitions(
        ReferralStatus from, ReferralStatus to, bool shouldSucceed)
    {
        var referral = new Referral { Status = from };

        if (shouldSucceed)
        {
            referral.TransitionTo(to);
            Assert.Equal(to, referral.Status);
            Assert.NotNull(referral.UpdatedAt);
        }
        else
        {
            Assert.Throws<InvalidReferralTransitionException>(() => referral.TransitionTo(to));
            Assert.Equal(from, referral.Status);
        }
    }

    [Fact]
    public void TransitionTo_Declined_ClearsPauseWithoutExtendingDeadline()
    {
        var now = DateTime.Now;
        var deadline = now.AddHours(4);
        var referral = new Referral
        {
            Status = ReferralStatus.Triaged,
            SlaDeadline = deadline
        };
        referral.PauseSla("WaitingOnPatient", now);

        referral.TransitionTo(ReferralStatus.Declined);

        Assert.Equal(ReferralStatus.Declined, referral.Status);
        Assert.False(referral.SlaPaused);
        Assert.Null(referral.SlaPausedAt);
        Assert.Null(referral.SlaPauseReason);
        Assert.Equal(deadline, referral.SlaDeadline);
    }

    [Fact]
    public void TransitionTo_Completed_ClearsPauseWithoutExtending()
    {
        var now = DateTime.Now;
        var deadline = now.AddHours(4);
        var referral = new Referral
        {
            Status = ReferralStatus.Booked,
            SlaDeadline = deadline
        };
        referral.PauseSla("WaitingOnPatient", now);
        referral.TransitionTo(ReferralStatus.Completed);

        Assert.Equal(ReferralStatus.Completed, referral.Status);
        Assert.False(referral.SlaPaused);
        Assert.Equal(deadline, referral.SlaDeadline);
    }
}
