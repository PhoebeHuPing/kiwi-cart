using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using KiwiCart.Infrastructure.StoreClients;
using Microsoft.Extensions.Logging;

namespace KiwiCart.Infrastructure.Services;

/// <summary>
/// Admin-only backfill of product_gtins. Deep-fetches from Woolworths (whose
/// search results already carry the barcode/GTIN) and upserts rows. Runs on
/// demand via the admin API, off the user search path.
/// </summary>
public class GtinBackfillService : IGtinBackfillService
{
    private readonly WoolworthsClient _woolworths;
    private readonly IEnumerable<IGtinLookupClient> _lookupClients;
    private readonly IProductGtinRepository _repository;
    private readonly ILogger<GtinBackfillService> _logger;

    // Foodstuffs store brands whose GTINs are resolved via detail endpoints.
    private static readonly string[] FoodstuffsBrands = { "PakNSave", "NewWorld" };

    public GtinBackfillService(
        WoolworthsClient woolworths,
        IEnumerable<IGtinLookupClient> lookupClients,
        IProductGtinRepository repository,
        ILogger<GtinBackfillService> logger)
    {
        _woolworths = woolworths;
        _lookupClients = lookupClients;
        _repository = repository;
        _logger = logger;
    }

    public async Task<GtinBackfillResult> BackfillWoolworthsByNameAsync(
        string searchTerm, CancellationToken ct = default)
    {
        // WoolworthsClient.SearchAsync already deep-paginates.
        var products = await _woolworths.SearchAsync(searchTerm, ct);
        return await UpsertAllAsync(products, ct);
    }

    public async Task<GtinBackfillResult> BackfillWoolworthsBySkuAsync(
        string sku, CancellationToken ct = default)
    {
        // The Woolworths search endpoint matches on the sku term; pick the
        // product whose product id equals the requested sku.
        var products = await _woolworths.SearchAsync(sku, ct);
        var match = products.Where(p => p.ProductId == sku).ToList();
        return await UpsertAllAsync(match, ct);
    }

    public async Task<GtinBackfillResult> BackfillMissingFoodstuffsGtinsAsync(
        int count, int delayMs, CancellationToken ct = default)
    {
        if (count <= 0)
            return new GtinBackfillResult(0, 0, 0, 0);

        var candidates = await _repository.GetMissingGtinRowsAsync(FoodstuffsBrands, count, ct);

        int updated = 0, skipped = 0;
        foreach (var row in candidates)
        {
            ct.ThrowIfCancellationRequested();

            // Find the lookup client for this row's store brand.
            var client = _lookupClients.FirstOrDefault(
                c => string.Equals(c.StoreBrand, row.StoreBrand, StringComparison.OrdinalIgnoreCase));
            if (client is null)
            {
                // No client can resolve this brand — mark attempted so it is
                // not re-fetched every batch.
                await _repository.MarkGtinLookupAttemptedAsync(row.StoreBrand, row.ExternalProductId, ct);
                skipped++;
                continue;
            }

            try
            {
                var rawGtin = await client.FetchGtinByProductIdAsync(row.ExternalProductId, ct);
                if (string.IsNullOrEmpty(rawGtin))
                {
                    skipped++;
                }
                else
                {
                    // Reuse the backfill upsert: it normalizes and only fills a
                    // row still missing its GTIN (never overwrites an existing one).
                    var price = new PriceResult
                    {
                        StoreBrand = row.StoreBrand,
                        ProductId = row.ExternalProductId,
                        Gtin = rawGtin,
                        ProductName = row.ProductName ?? string.Empty,
                        Brand = row.ProductBrand,
                        Volume = row.ProductSize
                    };
                    var outcome = await _repository.UpsertForBackfillAsync(price, ct);
                    if (outcome == GtinUpsertOutcome.Updated) updated++;
                    else skipped++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Missing-GTIN backfill failed for {Store}/{Pid}",
                    row.StoreBrand, row.ExternalProductId);
                skipped++;
            }
            finally
            {
                // Always mark the row as attempted (success or not) so a row
                // that cannot resolve a GTIN — e.g. weighed produce (KGM) with
                // no barcode — is not re-fetched on the next batch.
                await _repository.MarkGtinLookupAttemptedAsync(row.StoreBrand, row.ExternalProductId, ct);
            }

            // Rate-limit between upstream detail-endpoint calls.
            if (delayMs > 0)
                await Task.Delay(delayMs, ct);
        }

        return new GtinBackfillResult(candidates.Count, 0, updated, skipped);
    }

    private async Task<GtinBackfillResult> UpsertAllAsync(
        IReadOnlyList<PriceResult> products, CancellationToken ct)
    {
        int inserted = 0, updated = 0, skipped = 0;
        foreach (var p in products)
        {
            try
            {
                var outcome = await _repository.UpsertForBackfillAsync(p, ct);
                switch (outcome)
                {
                    case GtinUpsertOutcome.Inserted: inserted++; break;
                    case GtinUpsertOutcome.Updated: updated++; break;
                    default: skipped++; break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GTIN backfill failed for {Product}", p.ProductName);
                skipped++;
            }
        }
        return new GtinBackfillResult(products.Count, inserted, updated, skipped);
    }
}
