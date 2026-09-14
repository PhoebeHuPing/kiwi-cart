using System.Reflection;
using System.Text.Json;
using Dapper;
using Npgsql;

namespace KiwiCart.Infrastructure.Seed;

/// <summary>
/// A single product_gtins row in the portable seed. Ids are intentionally
/// excluded: rows are matched/deduped on (StoreBrand, ExternalProductId) so the
/// seed can be applied to any database without primary-key collisions.
/// </summary>
public record GtinSeedRow(
    string StoreBrand,
    string ExternalProductId,
    string? Gtin,
    string? ProductName,
    string? ProductBrand,
    string? ProductSize,
    bool NeedConfirm);

/// <summary>
/// Exports the local product_gtins table to a JSON seed file, and imports that
/// seed (shipped as an embedded resource) into a database. Import is
/// insert-if-absent: existing rows for a (store_brand, external_product_id) are
/// left untouched, so production data and manual edits are preserved.
/// </summary>
public static class GtinSeed
{
    /// <summary>Resource name of the embedded seed JSON.</summary>
    public const string ResourceName = "KiwiCart.Infrastructure.Seed.gtin-seed.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Read all product_gtins rows from the given connection and write them to
    /// a JSON file at <paramref name="outputPath"/>. Returns the row count.
    /// </summary>
    public static async Task<int> ExportAsync(string connectionString, string outputPath, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        var rows = (await connection.QueryAsync<GtinSeedRow>(
            @"SELECT store_brand AS StoreBrand, external_product_id AS ExternalProductId,
                     gtin AS Gtin, product_name AS ProductName, product_brand AS ProductBrand,
                     product_size AS ProductSize, need_confirm AS NeedConfirm
              FROM product_gtins
              ORDER BY store_brand, external_product_id")).ToList();

        var json = JsonSerializer.Serialize(rows, JsonOptions);
        await File.WriteAllTextAsync(outputPath, json, ct);
        return rows.Count;
    }

    /// <summary>
    /// Load the embedded seed rows. Returns an empty list if the resource is
    /// missing or empty.
    /// </summary>
    public static IReadOnlyList<GtinSeedRow> LoadEmbedded()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
            return [];

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<GtinSeedRow>>(json, JsonOptions) ?? [];
    }

    /// <summary>
    /// Insert the embedded seed rows into product_gtins, skipping any row whose
    /// (store_brand, external_product_id) already exists. Returns the number of
    /// rows inserted. Safe to run repeatedly (idempotent).
    /// </summary>
    public static int ImportEmbedded(NpgsqlConnection connection)
    {
        var rows = LoadEmbedded();
        if (rows.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        int inserted = 0;
        foreach (var r in rows)
        {
            inserted += connection.Execute(
                @"INSERT INTO product_gtins
                      (store_brand, external_product_id, gtin, product_name,
                       product_brand, product_size, need_confirm, created_at, updated_at)
                  VALUES (@StoreBrand, @ExternalProductId, @Gtin, @ProductName,
                          @ProductBrand, @ProductSize, @NeedConfirm, @Now, @Now)
                  ON CONFLICT (store_brand, external_product_id) DO NOTHING",
                new
                {
                    r.StoreBrand,
                    r.ExternalProductId,
                    r.Gtin,
                    r.ProductName,
                    r.ProductBrand,
                    r.ProductSize,
                    r.NeedConfirm,
                    Now = now
                });
        }
        return inserted;
    }

    /// <summary>
    /// Build idempotent INSERT statements for the embedded seed, for execution
    /// from an EF migration via MigrationBuilder.Sql(). Returns an empty string
    /// when there is nothing to seed. String literals are single-quote escaped.
    /// </summary>
    public static string BuildInsertSql()
    {
        var rows = LoadEmbedded();
        if (rows.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var r in rows)
        {
            sb.Append(
                "INSERT INTO product_gtins " +
                "(store_brand, external_product_id, gtin, product_name, product_brand, product_size, need_confirm, created_at, updated_at) VALUES (");
            sb.Append(Sql(r.StoreBrand)).Append(", ");
            sb.Append(Sql(r.ExternalProductId)).Append(", ");
            sb.Append(Sql(r.Gtin)).Append(", ");
            sb.Append(Sql(r.ProductName)).Append(", ");
            sb.Append(Sql(r.ProductBrand)).Append(", ");
            sb.Append(Sql(r.ProductSize)).Append(", ");
            sb.Append(r.NeedConfirm ? "true" : "false").Append(", ");
            sb.Append("now(), now()) ");
            sb.Append("ON CONFLICT (store_brand, external_product_id) DO NOTHING;");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // Render a nullable string as a SQL literal ('escaped' or NULL).
    private static string Sql(string? value)
        => value is null ? "NULL" : "'" + value.Replace("'", "''") + "'";
}
