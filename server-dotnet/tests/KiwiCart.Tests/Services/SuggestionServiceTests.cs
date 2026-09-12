using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace KiwiCart.Tests.Services;

public class SuggestionServiceTests
{
    private readonly Mock<IFavoritesService> _favorites = new();
    private readonly Mock<IGeminiClient> _gemini = new();
    private readonly Mock<IPriceComparisonService> _priceComparison = new();
    private readonly SuggestionService _sut;

    public SuggestionServiceTests()
    {
        _sut = new SuggestionService(
            _favorites.Object, _gemini.Object, _priceComparison.Object,
            NullLogger<SuggestionService>.Instance);
    }

    private void SetupFavorites(params string[] names) =>
        _favorites.Setup(f => f.GetFavoritesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(names.ToList());

    private void SetupGemini(string output) =>
        _gemini.Setup(g => g.GenerateContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(output);

    // Price results for a product, one per store name at the given price.
    private void SetupPrice(string product, params (string store, decimal price)[] prices) =>
        _priceComparison.Setup(p => p.CompareAsync(product, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prices
                .Select(pr => new PriceResult { ProductName = product, StoreName = pr.store, Price = pr.price })
                .OrderBy(r => r.Price)
                .ToList());

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsEmpty_WhenNoFavorites()
    {
        SetupFavorites();

        var result = await _sut.GetSuggestionsAsync("user1");

        Assert.Empty(result.Items);
        Assert.Equal(0m, result.TotalPotentialSaving);
        // AI must not be called when there is no history to personalize from.
        _gemini.Verify(
            g => g.GenerateContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSuggestionsAsync_PricesProposals_AndComputesCrossStoreSaving()
    {
        SetupFavorites("milk", "bread");
        SetupGemini("[{\"product\":\"milk\",\"reason\":\"A staple you buy often\"}," +
                    "{\"product\":\"eggs\",\"reason\":\"Goes well with bread\"}]");
        // milk: cheapest 3.00, next 3.50 -> saving 0.50
        SetupPrice("milk", ("PakNSave", 3.00m), ("NewWorld", 3.50m));
        // eggs: cheapest 6.00, next 6.20 -> saving 0.20
        SetupPrice("eggs", ("Woolworths", 6.20m), ("PakNSave", 6.00m));

        var result = await _sut.GetSuggestionsAsync("user1");

        Assert.Equal(2, result.Items.Count);

        var milk = result.Items[0];
        Assert.Equal("milk", milk.Product);
        Assert.Equal("A staple you buy often", milk.Reason);
        Assert.Equal(3.00m, milk.Cheapest!.Price);
        Assert.Equal(0.50m, milk.PotentialSaving);

        var eggs = result.Items[1];
        Assert.Equal(6.00m, eggs.Cheapest!.Price);
        Assert.Equal(0.20m, eggs.PotentialSaving);

        Assert.Equal(0.70m, result.TotalPotentialSaving);
    }

    [Fact]
    public async Task GetSuggestionsAsync_NoSaving_WhenSingleStore()
    {
        SetupFavorites("rice");
        SetupGemini("[{\"product\":\"rice\",\"reason\":\"A pantry staple\"}]");
        SetupPrice("rice", ("PakNSave", 4.00m));

        var result = await _sut.GetSuggestionsAsync("user1");

        var rice = Assert.Single(result.Items);
        Assert.NotNull(rice.Cheapest);
        Assert.Null(rice.PotentialSaving);
        Assert.Equal(0m, result.TotalPotentialSaving);
    }

    [Fact]
    public async Task GetSuggestionsAsync_KeepsItem_WhenNoPriceMatch()
    {
        SetupFavorites("saffron");
        SetupGemini("[{\"product\":\"saffron\",\"reason\":\"For your paella\"}]");
        SetupPrice("saffron"); // no store results

        var result = await _sut.GetSuggestionsAsync("user1");

        var item = Assert.Single(result.Items);
        Assert.Equal("saffron", item.Product);
        Assert.Null(item.Cheapest);
        Assert.Null(item.PotentialSaving);
    }

    [Fact]
    public async Task GetSuggestionsAsync_TolerantOfMarkdownFences_AndDedupes()
    {
        SetupFavorites("milk");
        SetupGemini("```json\n[{\"product\":\"Milk\",\"reason\":\"one\"}," +
                    "{\"product\":\"milk\",\"reason\":\"dup\"}," +
                    "{\"product\":\"butter\",\"reason\":\"two\"}]\n```");
        SetupPrice("Milk", ("PakNSave", 3.00m));
        SetupPrice("butter", ("NewWorld", 5.00m));

        var result = await _sut.GetSuggestionsAsync("user1");

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Milk", result.Items[0].Product);
        Assert.Equal("butter", result.Items[1].Product);
    }

    [Fact]
    public async Task GetSuggestionsAsync_CapsSuggestionCount()
    {
        SetupFavorites("milk");
        // 10 proposals; service caps at 6.
        var proposals = string.Join(",",
            Enumerable.Range(1, 10).Select(i => $"{{\"product\":\"item{i}\",\"reason\":\"r\"}}"));
        SetupGemini("[" + proposals + "]");
        _priceComparison
            .Setup(p => p.CompareAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>());

        var result = await _sut.GetSuggestionsAsync("user1");

        Assert.Equal(6, result.Items.Count);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsEmpty_WhenAiOutputUnparseable()
    {
        SetupFavorites("milk");
        SetupGemini("sorry, I cannot help with that");

        var result = await _sut.GetSuggestionsAsync("user1");

        Assert.Empty(result.Items);
        _priceComparison.Verify(
            p => p.CompareAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSuggestionsAsync_DefaultsReason_WhenMissing()
    {
        SetupFavorites("milk");
        SetupGemini("[{\"product\":\"milk\"}]");
        SetupPrice("milk", ("PakNSave", 3.00m));

        var result = await _sut.GetSuggestionsAsync("user1");

        var item = Assert.Single(result.Items);
        Assert.False(string.IsNullOrWhiteSpace(item.Reason));
    }
}
