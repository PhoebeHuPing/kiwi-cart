using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace KiwiCart.Tests.Services;

public class MealPlanServiceTests
{
    private readonly Mock<IGeminiClient> _gemini = new();
    private readonly Mock<IPriceComparisonService> _priceComparison = new();
    private readonly MealPlanService _sut;

    public MealPlanServiceTests()
    {
        _sut = new MealPlanService(
            _gemini.Object, _priceComparison.Object,
            NullLogger<MealPlanService>.Instance);
    }

    private void SetupGemini(string output) =>
        _gemini.Setup(g => g.GenerateContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(output);

    private void SetupPrice(string ingredient, params decimal[] prices) =>
        _priceComparison.Setup(p => p.CompareAsync(ingredient, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prices
                .Select(pr => new PriceResult { ProductName = ingredient, Price = pr })
                .OrderBy(r => r.Price)
                .ToList());

    [Fact]
    public async Task PlanAsync_ExtractsIngredients_AndPricesCheapest()
    {
        SetupGemini("chicken breast\nonion\nrice");
        SetupPrice("chicken breast", 12.00m, 9.50m);
        SetupPrice("onion", 2.00m);
        SetupPrice("rice", 4.20m, 3.80m);

        var result = await _sut.PlanAsync("chicken curry");

        Assert.Equal(3, result.Items.Count);
        Assert.Equal("chicken breast", result.Items[0].Ingredient);
        Assert.Equal(9.50m, result.Items[0].Cheapest!.Price); // cheapest picked
        Assert.Equal(2.00m, result.Items[1].Cheapest!.Price);
        Assert.Equal(3.80m, result.Items[2].Cheapest!.Price);
        Assert.Equal(9.50m + 2.00m + 3.80m, result.EstimatedTotal);
    }

    [Fact]
    public async Task PlanAsync_ParsesCommaSeparated_AndStripsListMarkers()
    {
        SetupGemini("1. Milk, - Bread, * Eggs");
        SetupPrice("Milk", 3.00m);
        SetupPrice("Bread", 2.50m);
        SetupPrice("Eggs", 6.00m);

        var result = await _sut.PlanAsync("breakfast");

        Assert.Equal(new[] { "Milk", "Bread", "Eggs" },
            result.Items.Select(i => i.Ingredient).ToArray());
    }

    [Fact]
    public async Task PlanAsync_DedupesCaseInsensitive()
    {
        SetupGemini("Onion\nonion\nONION\nGarlic");
        SetupPrice("Onion", 2.00m);
        SetupPrice("Garlic", 1.50m);

        var result = await _sut.PlanAsync("stir fry");

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Onion", result.Items[0].Ingredient);
        Assert.Equal("Garlic", result.Items[1].Ingredient);
    }

    [Fact]
    public async Task PlanAsync_ExcludesUnmatchedIngredientFromTotal_ButKeepsItem()
    {
        SetupGemini("saffron\nrice");
        SetupPrice("saffron"); // no matches -> empty list
        SetupPrice("rice", 3.80m);

        var result = await _sut.PlanAsync("paella");

        Assert.Equal(2, result.Items.Count);
        Assert.Null(result.Items[0].Cheapest);      // saffron unmatched
        Assert.NotNull(result.Items[1].Cheapest);
        Assert.Equal(3.80m, result.EstimatedTotal); // only rice counted
    }

    [Fact]
    public async Task PlanAsync_ReturnsEmpty_WhenNoIngredients()
    {
        SetupGemini("   ");

        var result = await _sut.PlanAsync("???");

        Assert.Empty(result.Items);
        Assert.Equal(0m, result.EstimatedTotal);
        _priceComparison.Verify(
            p => p.CompareAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PlanAsync_CapsIngredientCount()
    {
        // 20 distinct ingredients; service caps at 15.
        var lines = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"item{i}"));
        SetupGemini(lines);
        _priceComparison
            .Setup(p => p.CompareAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceResult>());

        var result = await _sut.PlanAsync("big list");

        Assert.Equal(15, result.Items.Count);
    }
}
