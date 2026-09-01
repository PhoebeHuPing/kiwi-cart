using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KiwiCart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feedback",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    message = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "stores",
                columns: new[] { "id", "address", "brand", "latitude", "longitude", "name" },
                values: new object[,]
                {
                    { 1, "Royal Oak, Auckland", "PakNSave", -36.909999999999997, 174.77600000000001, "Pak'nSave Royal Oak" },
                    { 2, "Victoria St West, Auckland CBD", "NewWorld", -36.848500000000001, 174.75210000000001, "New World Victoria Park" },
                    { 3, "Quay St, Auckland CBD", "Woolworths", -36.847499999999997, 174.767, "Woolworths Auckland City" },
                    { 4, "Don McKinnon Dr, Albany", "PakNSave", -36.726199999999999, 174.70609999999999, "Pak'nSave Albany" },
                    { 5, "Dominion Rd, Mt Eden", "NewWorld", -36.883699999999997, 174.76220000000001, "New World Mt Eden" },
                    { 6, "Broadway, Newmarket", "Woolworths", -36.868699999999997, 174.77699999999999, "Woolworths Newmarket" },
                    { 7, "New North Rd, Mt Albert", "PakNSave", -36.881700000000002, 174.71879999999999, "Pak'nSave Mt Albert" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feedback");

            migrationBuilder.DeleteData(
                table: "stores",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "stores",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "stores",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "stores",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "stores",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "stores",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "stores",
                keyColumn: "id",
                keyValue: 7);
        }
    }
}
