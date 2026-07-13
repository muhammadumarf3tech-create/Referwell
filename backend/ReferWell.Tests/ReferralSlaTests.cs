using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Domain.Exceptions;

namespace ReferWell.Tests;

public class ReferralSlaTests
{
    [Theory]
    [InlineData(UrgencyLevel.Urgent, 24)]
    [InlineData(UrgencyLevel.SemiUrgent, 168)]
    [InlineData(UrgencyLevel.Routine, 720)]
    public void CalculateSlaDeadline_ReturnsExpectedHours(UrgencyLevel urgency, int expectedHours)
    {
        var receivedAt = DateTime.Now;
        var deadline = Referral.CalculateSlaDeadline(urgency, receivedAt);
        var diff = deadline - receivedAt;

        Assert.Equal(expectedHours, Math.Round(diff.TotalHours));
    }

    [Fact]
    public void EvaluateSlaBreach_MarksReceivedPastDeadline()
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
    public void EvaluateSlaBreach_DoesNotMarkWhenAlreadyTriaged()
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
    public void EvaluateSlaBreach_ClearsWhenDeadlineExtended()
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
    public void EvaluateSlaBreach_AlreadyBreached_ReturnsFalse()
    {
        var now = DateTime.Now;
        var referral = new Referral
        {
            Status = ReferralStatus.Received,
            SlaDeadline = now.AddHours(-1),
            SlaBreach = true
        };

        Assert.False(referral.EvaluateSlaBreach(now));
        Assert.True(referral.SlaBreach);
    }

    [Fact]
    public void PauseSla_FreezesClockAndSkipsBreach()
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
    public void PauseSla_DefaultsEmptyReasonAndTruncatesLongReason()
    {
        var referral = new Referral
        {
            Status = ReferralStatus.Received,
            SlaDeadline = DateTime.Now.AddDays(1)
        };

        referral.PauseSla("   ");
        Assert.Equal("WaitingOnPatient", referral.SlaPauseReason);

        referral.ResumeSla();
        var longReason = new string('x', 150);
        referral.PauseSla(longReason);
        Assert.Equal(100, referral.SlaPauseReason!.Length);
    }

    [Fact]
    public void ResumeSla_ExtendsDeadlineByPausedDuration()
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
    public void ResumeSla_WhenNotPaused_Throws()
    {
        var referral = new Referral { Status = ReferralStatus.Received, SlaDeadline = DateTime.Now.AddDays(1) };

        Assert.Throws<InvalidSlaPauseException>(() => referral.ResumeSla());
    }

    [Fact]
    public void PauseSla_RejectsWhenAlreadyPausedOrClosed()
    {
        var referral = new Referral { Status = ReferralStatus.Received, SlaDeadline = DateTime.Now.AddDays(1) };
        referral.PauseSla();
        Assert.Throws<InvalidSlaPauseException>(() => referral.PauseSla());

        var closed = new Referral { Status = ReferralStatus.Completed, SlaDeadline = DateTime.Now.AddDays(1) };
        Assert.Throws<InvalidSlaPauseException>(() => closed.PauseSla());
    }

    [Fact]
    public void IsActivelySlaBreached_FalseWhenPausedEvenIfBreachFlagSet()
    {
        var now = DateTime.Now;
        var referral = new Referral
        {
            Status = ReferralStatus.Received,
            SlaDeadline = now.AddHours(-1),
            SlaBreach = true
        };
        referral.PauseSla("WaitingOnPatient", now);

        Assert.True(referral.SlaBreach);
        Assert.False(referral.IsActivelySlaBreached);
    }

    [Fact]
    public void IsActivelySlaBreached_TrueWhenBreachedAndNotPaused()
    {
        var referral = new Referral
        {
            Status = ReferralStatus.Received,
            SlaDeadline = DateTime.Now.AddHours(-1),
            SlaBreach = true,
            SlaPaused = false
        };

        Assert.True(referral.IsActivelySlaBreached);
    }
}
