using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiwiCart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGtinToFavorite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gtin",
                table: "favorites",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gtin",
                table: "favorites");
        }
    }
}
