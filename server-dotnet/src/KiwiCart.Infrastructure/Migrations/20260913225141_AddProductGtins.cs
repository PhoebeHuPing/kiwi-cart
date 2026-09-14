using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KiwiCart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductGtins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_gtins",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    gtin = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    product_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    product_brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    product_size = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    need_confirm = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_gtins", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_gtins_gtin",
                table: "product_gtins",
                column: "gtin");

            migrationBuilder.CreateIndex(
                name: "IX_product_gtins_store_brand_external_product_id",
                table: "product_gtins",
                columns: new[] { "store_brand", "external_product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_gtins");
        }
    }
}
