using ReferWell.Domain.Services;

namespace ReferWell.Tests;

public class CaseNoGeneratorTests
{
    [Fact]
    public void Format_PadsToSixDigits()
    {
        Assert.Equal("Ref-000001", CaseNoGenerator.Format(1));
        Assert.Equal("Ref-000042", CaseNoGenerator.Format(42));
        Assert.Equal("Ref-123456", CaseNoGenerator.Format(123456));
    }

    [Fact]
    public void MaxSequence_IgnoresNullWhitespaceAndNonRefPrefixes()
    {
        var existing = new string?[] { null, "  ", "LEGACY-99", "ABC-000099", "Ref-000007" };

        Assert.Equal(7, CaseNoGenerator.MaxSequence(existing));
    }

    [Fact]
    public void MaxSequence_IsCaseInsensitiveOnPrefix()
    {
        Assert.Equal(15, CaseNoGenerator.MaxSequence(["ref-000015", "REF-000003"]));
    }

    [Fact]
    public void Next_FromMaxSequence_IncrementsByOne()
    {
        Assert.Equal("Ref-000001", CaseNoGenerator.Next(0));
        Assert.Equal("Ref-000100", CaseNoGenerator.Next(99));
    }

    [Fact]
    public void Next_FromExisting_UsesHighestRefSequence()
    {
        var existing = new[] { "Ref-000008", "LEGACY-99", "Ref-000003", "ref-000012" };

        Assert.Equal(12, CaseNoGenerator.MaxSequence(existing));
        Assert.Equal("Ref-000013", CaseNoGenerator.Next(existing));
        Assert.Equal("Ref-000001", CaseNoGenerator.Next(Array.Empty<string>()));
    }
}
