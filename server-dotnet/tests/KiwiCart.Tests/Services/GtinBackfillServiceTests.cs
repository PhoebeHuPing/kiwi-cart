using KiwiCart.Core.DTOs;
using KiwiCart.Core.Entities;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.Services;
using KiwiCart.Infrastructure.StoreClients;
using KiwiCart.Infrastructure.TokenProviders;
using KiwiCart.Infrastructure.Data;
using KiwiCart.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace KiwiCart.Tests.Services;

public class GtinBackfillServiceTests
{
    // Backfill-by-name: aggregates repository outcomes into a result summary.
    [Fact]
    public async Task BackfillByName_CountsInsertsUpdatesSkips()
    {
        var products = new List<PriceResult>
        {
            new() { StoreBrand = "Woolworths", ProductId = "1", Gtin = "94152210" },
            new() { StoreBrand = "Woolworths", ProductId = "2", Gtin = "94154672" },
            new() { StoreBrand = "Woolworths", ProductId = "3", Gtin = "94127317" },
        };

        var woolworths = new StubWoolworthsClient(products);
        var repo = new Mock<IProductGtinRepository>();
        repo.SetupSequence(r => r.UpsertForBackfillAsync(It.IsAny<PriceResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GtinUpsertOutcome.Inserted)
            .ReturnsAsync(GtinUpsertOutcome.Updated)
            .ReturnsAsync(GtinUpsertOutcome.Skipped);

        var svc = new GtinBackfillService(woolworths, Array.Empty<IGtinLookupClient>(), repo.Object, NullLogger<GtinBackfillService>.Instance);
        var result = await svc.BackfillWoolworthsByNameAsync("milk");

        Assert.Equal(3, result.Fetched);
        Assert.Equal(1, result.Inserted);
        Assert.Equal(1, result.Updated);
        Assert.Equal(1, result.Skipped);
    }

    // Backfill-by-sku: only the product whose id matches the sku is upserted.
    [Fact]
    public async Task BackfillBySku_OnlyUpsertsMatchingProduct()
    {
        var products = new List<PriceResult>
        {
            new() { StoreBrand = "Woolworths", ProductId = "282768", Gtin = "94152210" },
            new() { StoreBrand = "Woolworths", ProductId = "999999", Gtin = "94154672" },
        };

        var woolworths = new StubWoolworthsClient(products);
        var repo = new Mock<IProductGtinRepository>();
        repo.Setup(r => r.UpsertForBackfillAsync(It.IsAny<PriceResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GtinUpsertOutcome.Inserted);

        var svc = new GtinBackfillService(woolworths, Array.Empty<IGtinLookupClient>(), repo.Object, NullLogger<GtinBackfillService>.Instance);
        var result = await svc.BackfillWoolworthsBySkuAsync("282768");

        Assert.Equal(1, result.Fetched);
        Assert.Equal(1, result.Inserted);
        // Only the matching product id was passed to the repository.
        repo.Verify(r => r.UpsertForBackfillAsync(
            It.Is<PriceResult>(p => p.ProductId == "282768"), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.UpsertForBackfillAsync(
            It.Is<PriceResult>(p => p.ProductId == "999999"), It.IsAny<CancellationToken>()), Times.Never);
    }

    // API 3: resolve missing Foodstuffs GTINs via detail-endpoint lookup clients.
    [Fact]
    public async Task BackfillMissingFoodstuffs_ResolvesAndUpdates()
    {
        var candidates = new List<ProductGtin>
        {
            new() { StoreBrand = "PakNSave", ExternalProductId = "5000527-EA-000" }, // resolves
            new() { StoreBrand = "NewWorld", ExternalProductId = "5000522-EA-000" }, // no gtin found
            new() { StoreBrand = "Unknown",  ExternalProductId = "x" },              // no client
        };

        var repo = new Mock<IProductGtinRepository>();
        repo.Setup(r => r.GetMissingGtinRowsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        repo.Setup(r => r.UpsertForBackfillAsync(It.IsAny<PriceResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GtinUpsertOutcome.Updated);

        var pak = new Mock<IGtinLookupClient>();
        pak.SetupGet(c => c.StoreBrand).Returns("PakNSave");
        pak.Setup(c => c.FetchGtinByProductIdAsync("5000527-EA-000", It.IsAny<CancellationToken>()))
            .ReturnsAsync("94152210");

        var nw = new Mock<IGtinLookupClient>();
        nw.SetupGet(c => c.StoreBrand).Returns("NewWorld");
        nw.Setup(c => c.FetchGtinByProductIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null); // GTIN not found

        var woolworths = new StubWoolworthsClient(new List<PriceResult>());
        var svc = new GtinBackfillService(
            woolworths, new[] { pak.Object, nw.Object }, repo.Object,
            NullLogger<GtinBackfillService>.Instance);

        var result = await svc.BackfillMissingFoodstuffsGtinsAsync(count: 10, delayMs: 0);

        Assert.Equal(3, result.Fetched);
        Assert.Equal(1, result.Updated);  // only the PakNSave row resolved
        Assert.Equal(2, result.Skipped);  // NewWorld (no gtin) + Unknown (no client)

        // Every candidate must be marked attempted so it is not re-fetched next
        // batch — including the ones that resolved no GTIN (KGM-style rows).
        repo.Verify(r => r.MarkGtinLookupAttemptedAsync("PakNSave", "5000527-EA-000", It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.MarkGtinLookupAttemptedAsync("NewWorld", "5000522-EA-000", It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.MarkGtinLookupAttemptedAsync("Unknown", "x", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// A WoolworthsClient whose deep search is overridden to return a fixed list,
    /// so backfill logic can be tested without HTTP.
    /// </summary>
    private sealed class StubWoolworthsClient : WoolworthsClient
    {
        private readonly IReadOnlyList<PriceResult> _products;

        public StubWoolworthsClient(IReadOnlyList<PriceResult> products)
            : base(CreateTokenProvider(), new Mock<IHttpClientFactory>().Object,
                NullLogger<WoolworthsClient>.Instance)
        {
            _products = products;
        }

        protected override Task<IReadOnlyList<PriceResult>?> ExecuteSearchAsync(
            string term, string token, CancellationToken ct, string? storeId = null)
            => Task.FromResult<IReadOnlyList<PriceResult>?>(_products);

        private static WoolworthsTokenProvider CreateTokenProvider()
            => new FakeWoolworthsTokenProvider("cookie");
    }
}
