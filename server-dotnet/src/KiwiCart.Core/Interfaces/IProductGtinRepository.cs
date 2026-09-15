using KiwiCart.Core.DTOs;
using KiwiCart.Core.Entities;

namespace KiwiCart.Core.Interfaces;

public interface IProductGtinRepository
{
    /// <summary>
    /// Insert a GTIN row for a store's product if one does not already exist
    /// for (StoreBrand, ExternalProductId). Existing rows are left untouched.
    /// The price's raw <see cref="PriceResult.Gtin"/> is normalized first;
    /// rows without a valid GTIN or product id are skipped. Returns true when a
    /// new row was inserted.
    /// </summary>
    Task<bool> InsertIfAbsentAsync(PriceResult price, CancellationToken ct = default);

    /// <summary>
    /// Admin backfill upsert. Inserts a new row, or updates an existing row for
    /// (StoreBrand, ExternalProductId) when it is missing a GTIN by filling in
    /// the newly-fetched GTIN/name/brand/size. Rows that already have a GTIN are
    /// left untouched (never overwrite a confirmed value). A product id is
    /// required; the GTIN may be null (row recorded without a GTIN for later).
    /// Returns the outcome so callers can count inserts vs updates vs skips.
    /// </summary>
    Task<GtinUpsertOutcome> UpsertForBackfillAsync(PriceResult price, CancellationToken ct = default);

    /// <summary>
    /// Return up to <paramref name="limit"/> rows for the given store brands
    /// that do not yet have a GTIN (gtin IS NULL) and have not had a lookup
    /// attempted yet, oldest first. Used by the admin backfill to resolve
    /// missing Foodstuffs GTINs via detail endpoints.
    /// </summary>
    Task<IReadOnlyList<ProductGtin>> GetMissingGtinRowsAsync(
        IReadOnlyCollection<string> storeBrands, int limit, CancellationToken ct = default);

    /// <summary>
    /// Mark a row (by store brand + product id) as having had a GTIN lookup
    /// attempted, so it is not repeatedly re-fetched when the lookup cannot
    /// resolve a GTIN (e.g. weighed produce with no barcode).
    /// </summary>
    Task MarkGtinLookupAttemptedAsync(
        string storeBrand, string externalProductId, CancellationToken ct = default);

    /// <summary>
    /// Batch-resolve GTINs for a set of (store brand, product id) pairs. Returns
    /// a map keyed by "{storeBrand}|{externalProductId}" to the stored GTIN.
    /// Pairs without a row or without a GTIN are omitted. Used to merge search
    /// results across platforms by GTIN.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetGtinsForAsync(
        IReadOnlyCollection<(string StoreBrand, string ExternalProductId)> keys,
        CancellationToken ct = default);

    /// <summary>
    /// Reverse lookup: given a GTIN, return all (store brand, product id) rows
    /// that share that GTIN. This is the cross-platform mapping used to price a
    /// single product across every supermarket that stocks it. The GTIN is
    /// normalized before matching. Returns an empty list when none is found.
    /// </summary>
    Task<IReadOnlyList<ProductGtin>> GetByGtinAsync(string gtin, CancellationToken ct = default);
}

/// <summary>Result of a single backfill upsert.</summary>
public enum GtinUpsertOutcome
{
    Skipped,   // no product id, or nothing to change
    Inserted,  // a new row was created
    Updated    // an existing row had its GTIN backfilled
}
