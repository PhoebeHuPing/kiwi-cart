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
            migrationBuilder.AddColumn<string>(
                name: "external_store_id",
                table: "stores",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "id",
                keyValue: 1,
                column: "external_store_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "id",
                keyValue: 2,
                column: "external_store_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "id",
                keyValue: 3,
                column: "external_store_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "id",
                keyValue: 4,
                column: "external_store_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "id",
                keyValue: 5,
                column: "external_store_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "id",
                keyValue: 6,
                column: "external_store_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "id",
                keyValue: 7,
                column: "external_store_id",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_stores_brand_external_store_id",
                table: "stores",
                columns: new[] { "brand", "external_store_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stores_brand_external_store_id",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "external_store_id",
                table: "stores");
        }
    }
}
