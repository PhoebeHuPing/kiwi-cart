using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiwiCart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIdVolumeUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_product_id",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unit_price",
                table: "prices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "volume",
                table: "prices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "external_product_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "prices");

            migrationBuilder.DropColumn(
                name: "volume",
                table: "prices");
        }
    }
}
