using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiwiCart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalStoreId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: this migration may run against a database where the
            // column/index were already added out-of-band (e.g. a shared CI
            // database). Guard with IF NOT EXISTS so a re-run cannot fail with
            // "column already exists" (Postgres 42701).
            migrationBuilder.Sql(
                "ALTER TABLE stores ADD COLUMN IF NOT EXISTS external_store_id character varying(100);");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_stores_brand_external_store_id\" " +
                "ON stores (brand, external_store_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_stores_brand_external_store_id\";");

            migrationBuilder.Sql(
                "ALTER TABLE stores DROP COLUMN IF EXISTS external_store_id;");
        }
    }
}
