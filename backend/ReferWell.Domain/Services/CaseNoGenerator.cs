namespace ReferWell.Domain.Services;

/// <summary>
/// Generates sequential referral case numbers (Ref-000001, Ref-000002, …).
/// Uses the highest existing Ref-###### suffix + 1 (not CreatedAt), so legacy
/// or imported non-Ref case numbers cannot reset the sequence back to 000001.
/// </summary>
public static class CaseNoGenerator
{
    public const string Prefix = "Ref-";

    public static string Format(int number) => $"{Prefix}{number:D6}";

    public static int MaxSequence(IEnumerable<string?> caseNos)
    {
        var max = 0;
        foreach (var caseNo in caseNos)
        {
            if (string.IsNullOrWhiteSpace(caseNo))
                continue;
            if (!caseNo.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            if (int.TryParse(caseNo.AsSpan(Prefix.Length), out var n) && n > max)
                max = n;
        }
        return max;
    }

    public static string Next(IEnumerable<string?> existingCaseNos) =>
        Format(MaxSequence(existingCaseNos) + 1);

    public static string Next(int currentMaxSequence) =>
        Format(currentMaxSequence + 1);
}
