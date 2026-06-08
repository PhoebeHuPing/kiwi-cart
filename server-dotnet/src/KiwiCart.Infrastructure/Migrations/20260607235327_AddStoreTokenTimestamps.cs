using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiwiCart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreTokenTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "issued_at",
                table: "store_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "store_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "issued_at",
                table: "store_tokens");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "store_tokens");
        }
    }
}
