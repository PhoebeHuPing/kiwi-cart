using KiwiCart.Core.DTOs;
using KiwiCart.Infrastructure.StoreClients;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KiwiCart.Tests.StoreClients;

/// <summary>
/// Unit tests for StoreApiClient.NormalizeProductName. The method is a
/// protected static helper shared by all store clients (it prepends the brand
/// to the product name when the name does not already contain it). We expose it
/// here through a minimal test subclass.
/// </summary>
public class NormalizeProductNameTests
{
    [Theory]
    // Brand missing from name → prepended.
    [InlineData("Blue Milk", "Anchor", "Anchor Blue Milk")]
    // Name already starts with brand → unchanged (no duplication).
    [InlineData("Anchor Blue Milk", "Anchor", "Anchor Blue Milk")]
    // Brand present mid-name (case-insensitive) → unchanged.
    [InlineData("Fresh ANCHOR Milk", "anchor", "Fresh ANCHOR Milk")]
    // Null brand → name returned untouched.
    [InlineData("Blue Milk", null, "Blue Milk")]
    // Empty brand → name returned untouched.
    [InlineData("Blue Milk", "", "Blue Milk")]
    // Empty product name → returned untouched (even with a brand).
    [InlineData("", "Anchor", "")]
    public void NormalizeProductName_HandlesBrandPrefixing(
        string productName, string? brand, string expected)
    {
        var actual = TestableStoreClient.CallNormalize(productName, brand);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Minimal concrete StoreApiClient that exposes the protected static
    /// NormalizeProductName for direct unit testing.
    /// </summary>
    private sealed class TestableStoreClient : StoreApiClient
    {
        private TestableStoreClient()
            : base(null!, NullLogger<TestableStoreClient>.Instance) { }

        public override string StoreName => "Test";
        public override string StoreBrand => "Test";

        protected override Task<IReadOnlyList<PriceResult>?> ExecuteSearchAsync(
            string term, string token, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<PriceResult>?>(new List<PriceResult>());

        // NormalizeProductName is a protected static helper on the base class.
        public static string CallNormalize(string productName, string? brand)
            => NormalizeProductName(productName, brand);
    }
}
