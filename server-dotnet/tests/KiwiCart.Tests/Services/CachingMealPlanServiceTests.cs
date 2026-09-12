using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KiwiCart.Tests.Services;

public class CachingMealPlanServiceTests
{
    private readonly Mock<IMealPlanService> _inner = new();

    private CachingMealPlanService CreateSut(
        MealPlanCacheOptions? options = null,
        IMemoryCache? cache = null)
    {
        options ??= new MealPlanCacheOptions();
        cache ??= new MemoryCache(new MemoryCacheOptions { SizeLimit = options.MaxEntries });
        return new CachingMealPlanService(
            _inner.Object,
            cache,
            Options.Create(options),
            NullLogger<CachingMealPlanService>.Instance);
    }

    private static MealPlanResponse ResponseWith(params string[] ingredients) =>
        new()
        {
            Items = ingredients
                .Select(i => new MealPlanItem { Ingredient = i, Cheapest = new PriceResult { ProductName = i, Price = 1m } })
                .ToList(),
            EstimatedTotal = ingredients.Length,
        };

    [Fact]
    public async Task PlanAsync_CachesResult_SecondCallDoesNotHitInner()
    {
        _inner.Setup(s => s.PlanAsync("chicken curry", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResponseWith("chicken", "onion"));
        var sut = CreateSut();

        var first = await sut.PlanAsync("chicken curry");
        var second = await sut.PlanAsync("chicken curry");

        Assert.Same(first, second);
        _inner.Verify(s => s.PlanAsync("chicken curry", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlanAsync_NormalizesPrompt_CaseAndWhitespaceShareCacheEntry()
    {
        _inner.Setup(s => s.PlanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResponseWith("rice"));
        var sut = CreateSut();

        await sut.PlanAsync("Chicken Curry");
        await sut.PlanAsync("  chicken curry  ");

        // Both prompts normalize to the same key -> inner invoked only once.
        _inner.Verify(s => s.PlanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlanAsync_DifferentPrompts_CachedSeparately()
    {
        _inner.Setup(s => s.PlanAsync("breakfast", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResponseWith("eggs"));
        _inner.Setup(s => s.PlanAsync("dinner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResponseWith("steak"));
        var sut = CreateSut();

        await sut.PlanAsync("breakfast");
        await sut.PlanAsync("dinner");

        _inner.Verify(s => s.PlanAsync("breakfast", It.IsAny<CancellationToken>()), Times.Once);
        _inner.Verify(s => s.PlanAsync("dinner", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlanAsync_DoesNotCacheEmptyResult()
    {
        _inner.Setup(s => s.PlanAsync("???", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MealPlanResponse { Items = [], EstimatedTotal = 0m });
        var sut = CreateSut();

        await sut.PlanAsync("???");
        await sut.PlanAsync("???");

        // Empty results are not pinned, so inner is retried each time.
        _inner.Verify(s => s.PlanAsync("???", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PlanAsync_WhenDisabled_AlwaysHitsInner()
    {
        _inner.Setup(s => s.PlanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResponseWith("rice"));
        var sut = CreateSut(new MealPlanCacheOptions { Enabled = false });

        await sut.PlanAsync("chicken curry");
        await sut.PlanAsync("chicken curry");

        _inner.Verify(s => s.PlanAsync("chicken curry", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PlanAsync_NonPositiveTtl_DisablesCaching()
    {
        _inner.SetupSequence(s => s.PlanAsync("soup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResponseWith("carrot"))
            .ReturnsAsync(ResponseWith("carrot", "celery"));
        // TtlMinutes <= 0 must be treated as "no caching" and never throw
        // (MemoryCache rejects a zero/negative relative expiry).
        var sut = CreateSut(new MealPlanCacheOptions { TtlMinutes = 0 });

        var first = await sut.PlanAsync("soup");
        var second = await sut.PlanAsync("soup");

        Assert.Single(first.Items);
        Assert.Equal(2, second.Items.Count);
        _inner.Verify(s => s.PlanAsync("soup", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
