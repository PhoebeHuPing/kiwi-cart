using KiwiCart.Core.Utils;
using Xunit;

namespace KiwiCart.Tests.Utils;

/// <summary>
/// Tests for GtinNormalizer. The real examples come from live API captures:
/// the same Anchor Blue Milk shows GTIN-8 "94152210" on both Foodstuffs (sku)
/// and Woolworths (barcode); Pams own-brand lines use GTIN-13 such as
/// "9415077379187". Both must normalize to the same 14-digit canonical form so
/// cross-platform matching works.
/// </summary>
public class GtinNormalizerTests
{
    [Theory]
    // GTIN-8 (branded lines like Anchor) → zero-padded to 14.
    [InlineData("94152210", "00000094152210")]
    [InlineData("94154672", "00000094154672")]
    // GTIN-13 (Pams own-brand) → zero-padded to 14.
    [InlineData("9415077379187", "09415077379187")]
    [InlineData("9415077177066", "09415077177066")]
    // Already 14 digits → unchanged (valid check digit).
    [InlineData("09415077379187", "09415077379187")]
    // Surrounding whitespace is trimmed.
    [InlineData("  94152210  ", "00000094152210")]
    public void Normalize_ValidGtins_PadToFourteen(string input, string expected)
    {
        Assert.Equal(expected, GtinNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    // Not all digits.
    [InlineData("94152210X")]
    [InlineData("abc")]
    // Unsupported length (9, 10, 11 digits are not valid GTIN lengths).
    [InlineData("123456789")]
    [InlineData("9415077379")]
    // Valid length but wrong check digit (last digit changed from 0 to 1).
    [InlineData("94152211")]
    [InlineData("9415077379188")]
    public void Normalize_InvalidInput_ReturnsNull(string? input)
    {
        Assert.Null(GtinNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_SameProductAcrossPlatforms_YieldsSameCanonicalGtin()
    {
        // Foodstuffs sku and Woolworths barcode for the same Anchor Blue Milk.
        var foodstuffs = GtinNormalizer.Normalize("94152210");
        var woolworths = GtinNormalizer.Normalize("94152210");

        Assert.NotNull(foodstuffs);
        Assert.Equal(foodstuffs, woolworths);
    }

    [Theory]
    [InlineData("94152210", true)]
    [InlineData("9415077379187", true)]
    [InlineData("94152211", false)] // bad check digit
    [InlineData("notagtin", false)]
    [InlineData(null, false)]
    public void IsValid_MatchesNormalizeOutcome(string? input, bool expected)
    {
        Assert.Equal(expected, GtinNormalizer.IsValid(input));
    }
}
