using KiwiCart.Infrastructure.Repositories;
using Xunit;

namespace KiwiCart.Tests.Repositories;

/// <summary>
/// Unit tests for PriceCacheRepository.BuildDisplayName. This is the logic that
/// regenerates a cached product's display name from its stored brand + name.
/// It deliberately uses StartsWith (not Contains) so a name that only mentions
/// the brand mid-string still gets a leading brand, while a name already led by
/// the brand is not duplicated.
/// </summary>
public class PriceCacheDisplayNameTests
{
    [Theory]
    // Brand missing from the front of the name → prepended.
    [InlineData("Blue Milk", "Anchor", "Anchor Blue Milk")]
    // Name already starts with the brand → unchanged (no duplication).
    [InlineData("Anchor Blue Milk", "Anchor", "Anchor Blue Milk")]
    // StartsWith is case-insensitive → still treated as already-prefixed.
    [InlineData("anchor blue milk", "Anchor", "anchor blue milk")]
    // Brand appears mid-name but NOT at the start → still prepended, because we
    // use StartsWith rather than Contains. This is the key behavioural
    // difference from NormalizeProductName.
    [InlineData("Fresh Anchor Milk", "Anchor", "Anchor Fresh Anchor Milk")]
    // Null brand → name returned untouched.
    [InlineData("Blue Milk", null, "Blue Milk")]
    // Empty brand → name returned untouched.
    [InlineData("Blue Milk", "", "Blue Milk")]
    public void BuildDisplayName_PrefixesBrandUnlessNameAlreadyStartsWithIt(
        string productName, string? brand, string expected)
    {
        var actual = PriceCacheRepository.BuildDisplayName(productName, brand);
        Assert.Equal(expected, actual);
    }
}
