using ReferWell.Domain.Enums;
using ReferWell.Domain.Services;

namespace ReferWell.Tests;

public class PriorityCalculatorTests
{
    [Fact]
    public void Calculate_UrgentYoungPatient_RecentReferral_IsInExpectedBand()
    {
        var receivedAt = DateTime.Now;
        var dob = DateTime.Now.AddYears(-40);

        // Urgent (100) @ 50% + 0 wait @ 30% + ~44 age @ 20% ≈ 50–65
        var score = PriorityCalculator.Calculate(
            UrgencyLevel.Urgent,
            receivedAt,
            dob,
            weightUrgency: 50,
            weightWaitTime: 30,
            weightPatient: 20);

        Assert.InRange(score, 50, 65);
    }

    [Theory]
    [InlineData(UrgencyLevel.Routine, UrgencyLevel.SemiUrgent)]
    [InlineData(UrgencyLevel.SemiUrgent, UrgencyLevel.Urgent)]
    public void Calculate_HigherUrgency_ProducesHigherScore(UrgencyLevel lower, UrgencyLevel higher)
    {
        var receivedAt = DateTime.Now.AddDays(-5);
        var dob = DateTime.Now.AddYears(-50);

        var lowerScore = PriorityCalculator.Calculate(lower, receivedAt, dob);
        var higherScore = PriorityCalculator.Calculate(higher, receivedAt, dob);

        Assert.True(higherScore > lowerScore);
    }

    [Fact]
    public void Calculate_OlderPatient_ScoresHigherThanYounger()
    {
        var receivedAt = DateTime.Now.AddDays(-2);
        var young = DateTime.Now.AddYears(-25);
        var older = DateTime.Now.AddYears(-80);

        var youngScore = PriorityCalculator.Calculate(UrgencyLevel.Routine, receivedAt, young);
        var olderScore = PriorityCalculator.Calculate(UrgencyLevel.Routine, receivedAt, older);

        Assert.True(olderScore > youngScore);
    }

    [Fact]
    public void Calculate_LongerWait_ScoresHigher()
    {
        var dob = DateTime.Now.AddYears(-45);
        var recent = DateTime.Now.AddDays(-1);
        var older = DateTime.Now.AddDays(-45);

        var recentScore = PriorityCalculator.Calculate(UrgencyLevel.SemiUrgent, recent, dob);
        var olderScore = PriorityCalculator.Calculate(UrgencyLevel.SemiUrgent, older, dob);

        Assert.True(olderScore > recentScore);
    }

    [Fact]
    public void Calculate_WaitAndAge_AreCappedAtNinetyDaysAndYears()
    {
        var dob = DateTime.Now.AddYears(-120);
        var receivedAt = DateTime.Now.AddDays(-200);

        // With only patient + wait weights, max contribution is 100 each → capped score = 100
        var score = PriorityCalculator.Calculate(
            UrgencyLevel.Routine,
            receivedAt,
            dob,
            weightUrgency: 0,
            weightWaitTime: 50,
            weightPatient: 50);

        Assert.Equal(100, score, precision: 1);
    }
}
