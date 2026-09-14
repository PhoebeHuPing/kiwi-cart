using KiwiCart.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiwiCart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductGtins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Import the GTIN mapping data accumulated locally, shipped as an
            // embedded JSON resource. Inserts are idempotent
            // (ON CONFLICT (store_brand, external_product_id) DO NOTHING), so
            // any rows a production database already has (including manual
            // edits) are preserved.
            var sql = GtinSeed.BuildInsertSql();
            if (!string.IsNullOrEmpty(sql))
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: the seed only inserts absent rows and cannot know which
            // rows pre-existed, so we do not delete on rollback.
        }
    }
}
