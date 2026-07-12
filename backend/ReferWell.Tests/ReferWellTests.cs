using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Domain.Exceptions;
using ReferWell.Domain.Services;
using Xunit;

namespace ReferWell.Tests;

public class ReferWellTests
{
    [Fact]
    public void PriorityCalculator_ShouldCalculateCorrectScores()
    {
        // Arrange
        var receivedAt = DateTime.Now;
        var dob = DateTime.Now.AddYears(-40); // 40 years old

        // Act
        // Urgent (100 score) at weight 50% + 0 days wait at weight 30% + 40/90 age (44.4 score) at weight 20%
        var score = PriorityCalculator.Calculate(
            UrgencyLevel.Urgent,
            receivedAt,
            dob,
            weightUrgency: 50,
            weightWaitTime: 30,
            weightPatient: 20
        );

        // Assert
        Assert.True(score > 50);
        Assert.True(score < 65);
    }

    [Theory]
    [InlineData(ReferralStatus.Received, ReferralStatus.Triaged, true)]
    [InlineData(ReferralStatus.Received, ReferralStatus.Accepted, false)]
    [InlineData(ReferralStatus.Triaged, ReferralStatus.Accepted, true)]
    [InlineData(ReferralStatus.Triaged, ReferralStatus.Declined, true)]
    [InlineData(ReferralStatus.Accepted, ReferralStatus.Booked, true)]
    [InlineData(ReferralStatus.Booked, ReferralStatus.Completed, true)]
    public void Referral_StateMachine_ShouldEnforceAllowedTransitions(
        ReferralStatus from, ReferralStatus to, bool shouldSucceed)
    {
        // Arrange
        var referral = new Referral { Status = from };

        // Act & Assert
        if (shouldSucceed)
        {
            referral.TransitionTo(to);
            Assert.Equal(to, referral.Status);
        }
        else
        {
            Assert.Throws<InvalidReferralTransitionException>(() => referral.TransitionTo(to));
        }
    }

    [Fact]
    public void Referral_Claim_ShouldSetUserAndPreventOtherClaims()
    {
        // Arrange
        var referral = new Referral();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        // Act
        referral.Claim(user1);

        // Assert
        Assert.Equal(user1, referral.ClaimedByUserId);
        Assert.NotNull(referral.ClaimedAt);

        // Claiming by same user should be fine
        referral.Claim(user1);

        // Claiming by another user should fail
        Assert.Throws<ReferralAlreadyClaimedException>(() => referral.Claim(user2));
    }

    [Fact]
    public void Referral_Release_ShouldClearClaim()
    {
        // Arrange
        var referral = new Referral();
        var user = Guid.NewGuid();
        referral.Claim(user);

        // Act
        referral.Release();

        // Assert
        Assert.Null(referral.ClaimedByUserId);
        Assert.Null(referral.ClaimedAt);
    }

    [Theory]
    [InlineData(UrgencyLevel.Urgent, 24)]
    [InlineData(UrgencyLevel.SemiUrgent, 168)] // 7 days * 24
    [InlineData(UrgencyLevel.Routine, 720)] // 30 days * 24
    public void Referral_CalculateSlaDeadline_ShouldReturnCorrectDeadline(UrgencyLevel urgency, int expectedHours)
    {
        // Arrange
        var receivedAt = DateTime.Now;

        // Act
        var deadline = Referral.CalculateSlaDeadline(urgency, receivedAt);

        // Assert
        var diff = deadline - receivedAt;
        Assert.Equal(expectedHours, Math.Round(diff.TotalHours));
    }

    [Fact]
    public void Referral_EvaluateSlaBreach_MarksReceivedPastDeadline()
    {
        var now = DateTime.Now;
        var referral = new Referral
        {
            Status = ReferralStatus.Received,
            ReceivedAt = now.AddHours(-25),
            SlaDeadline = now.AddHours(-1),
            SlaBreach = false
        };

        var newlyBreached = referral.EvaluateSlaBreach(now);

        Assert.True(newlyBreached);
        Assert.True(referral.SlaBreach);
    }

    [Fact]
    public void Referral_EvaluateSlaBreach_DoesNotMarkWhenAlreadyTriaged()
    {
        var now = DateTime.Now;
        var referral = new Referral
        {
            Status = ReferralStatus.Triaged,
            ReceivedAt = now.AddHours(-25),
            SlaDeadline = now.AddHours(-1),
            SlaBreach = false
        };

        var newlyBreached = referral.EvaluateSlaBreach(now);

        Assert.False(newlyBreached);
        Assert.False(referral.SlaBreach);
    }

    [Fact]
    public void Referral_EvaluateSlaBreach_ClearsWhenDeadlineExtended()
    {
        var now = DateTime.Now;
        var referral = new Referral
        {
            Status = ReferralStatus.Received,
            ReceivedAt = now.AddHours(-2),
            SlaDeadline = now.AddHours(22),
            SlaBreach = true
        };

        referral.EvaluateSlaBreach(now);

        Assert.False(referral.SlaBreach);
    }

    [Fact]
    public void Referral_PauseSla_FreezesClockAndSkipsBreach()
    {
        var now = DateTime.Now;
        var referral = new Referral
        {
            Status = ReferralStatus.Received,
            ReceivedAt = now.AddHours(-25),
            SlaDeadline = now.AddHours(-1),
            SlaBreach = false
        };

        referral.PauseSla("WaitingOnPatient", now);

        Assert.True(referral.SlaPaused);
        Assert.Equal("WaitingOnPatient", referral.SlaPauseReason);
        Assert.False(referral.EvaluateSlaBreach(now.AddHours(2)));
        Assert.False(referral.SlaBreach);
    }

    [Fact]
    public void Referral_ResumeSla_ExtendsDeadlineByPausedDuration()
    {
        var now = DateTime.Now;
        var originalDeadline = now.AddHours(5);
        var referral = new Referral
        {
            Status = ReferralStatus.Received,
            ReceivedAt = now.AddHours(-1),
            SlaDeadline = originalDeadline,
            SlaBreach = false
        };

        referral.PauseSla("WaitingOnPatient", now);
        referral.ResumeSla(now.AddHours(3));

        Assert.False(referral.SlaPaused);
        Assert.Null(referral.SlaPausedAt);
        Assert.Equal(originalDeadline.AddHours(3), referral.SlaDeadline);
    }

    [Fact]
    public void Referral_PauseSla_RejectsWhenAlreadyPausedOrClosed()
    {
        var referral = new Referral { Status = ReferralStatus.Received, SlaDeadline = DateTime.Now.AddDays(1) };
        referral.PauseSla();
        Assert.Throws<InvalidSlaPauseException>(() => referral.PauseSla());

        var closed = new Referral { Status = ReferralStatus.Completed, SlaDeadline = DateTime.Now.AddDays(1) };
        Assert.Throws<InvalidSlaPauseException>(() => closed.PauseSla());
    }

    [Fact]
    public void Referral_TransitionToCompleted_ClearsPauseWithoutExtending()
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
