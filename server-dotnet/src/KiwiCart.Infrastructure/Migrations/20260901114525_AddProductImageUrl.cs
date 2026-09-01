using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiwiCart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: the column may already exist if it was created by the
            // Node/knex backend, which shares this database. EF's migration
            // history does not track that column, so guard with IF NOT EXISTS.
            migrationBuilder.Sql(
                "ALTER TABLE products ADD COLUMN IF NOT EXISTS image_url character varying(1000);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE products DROP COLUMN IF EXISTS image_url;");
        }
    }
}
