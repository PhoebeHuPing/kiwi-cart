using KiwiCart.Core.DTOs;
using KiwiCart.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace KiwiCart.Tests.Repositories;

/// <summary>
/// Tests for ProductGtinRepository.InsertIfAbsentAsync guard clauses. These
/// cases return before any database connection is opened, so they run without
/// a live database. The insert/skip-existing behaviour against a real table is
/// covered by the manual live verification, not here.
/// </summary>
public class ProductGtinRepositoryTests
{
    private static ProductGtinRepository CreateRepo()
    {
        // A syntactically valid but unused connection string. Guard clauses
        // must return before this is ever used to open a connection.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=none;Username=x;Password=y"
            })
            .Build();
        return new ProductGtinRepository(config);
    }

    [Fact]
    public async Task InsertIfAbsentAsync_ReturnsFalse_WhenStoreBrandMissing()
    {
        var repo = CreateRepo();
        var price = new PriceResult { StoreBrand = "", ProductId = "282768", Gtin = "94152210" };
        Assert.False(await repo.InsertIfAbsentAsync(price));
    }

    [Fact]
    public async Task InsertIfAbsentAsync_ReturnsFalse_WhenProductIdMissing()
    {
        var repo = CreateRepo();
        var price = new PriceResult { StoreBrand = "Woolworths", ProductId = null, Gtin = "94152210" };
        Assert.False(await repo.InsertIfAbsentAsync(price));
    }

    [Fact]
    public async Task InsertIfAbsentAsync_ReturnsFalse_WhenGtinMissing()
    {
        var repo = CreateRepo();
        var price = new PriceResult { StoreBrand = "Woolworths", ProductId = "282768", Gtin = null };
        Assert.False(await repo.InsertIfAbsentAsync(price));
    }

    [Fact]
    public async Task InsertIfAbsentAsync_ReturnsFalse_WhenGtinInvalidCheckDigit()
    {
        var repo = CreateRepo();
        // Valid length but wrong check digit -> normalization fails -> skip.
        var price = new PriceResult { StoreBrand = "Woolworths", ProductId = "282768", Gtin = "94152211" };
        Assert.False(await repo.InsertIfAbsentAsync(price));
    }
}
