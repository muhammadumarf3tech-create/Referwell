using ReferWell.Domain.Enums;

namespace ReferWell.Domain.Services;

/// <summary>
/// Pure domain service for calculating referral priority scores.
/// Score = (WeightUrgency * UrgencyValue) + (WeightWaitTime * WaitDays) + (WeightPatient * AgeScore)
/// All weights are percentages that must sum to 100.
/// </summary>
public static class PriorityCalculator
{
    public static double Calculate(
        UrgencyLevel urgency,
        DateTime receivedAt,
        DateTime patientDateOfBirth,
        double weightUrgency = 50,
        double weightWaitTime = 30,
        double weightPatient = 20,
        DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.Now;

        // Normalize urgency (1-3) to 0-100 scale
        double urgencyScore = urgency switch
        {
            UrgencyLevel.Routine    => 33,
            UrgencyLevel.SemiUrgent => 66,
            UrgencyLevel.Urgent     => 100,
            _                       => 0
        };

        // Wait time in days, capped at 90 days → normalized 0-100
        double waitDays = (now - receivedAt).TotalDays;
        double waitScore = Math.Min(waitDays / 90.0, 1.0) * 100;

        // Patient age: older patients → higher score (capped at 100 for 90+)
        int age = now.Year - patientDateOfBirth.Year;
        if (patientDateOfBirth.Date > now.AddYears(-age)) age--;
        double ageScore = Math.Min(age / 90.0, 1.0) * 100;

        return ((weightUrgency / 100.0) * urgencyScore)
             + ((weightWaitTime / 100.0) * waitScore)
             + ((weightPatient / 100.0) * ageScore);
    }
}
