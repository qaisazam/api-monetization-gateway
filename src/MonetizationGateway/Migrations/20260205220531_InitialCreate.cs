using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MonetizationGateway.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MonthlyQuota = table.Column<int>(type: "int", nullable: false),
                    RequestsPerSecond = table.Column<int>(type: "int", nullable: false),
                    MonthlyPriceUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TierId = table.Column<int>(type: "int", nullable: false),
                    ApiKeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Tiers_TierId",
                        column: x => x.TierId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiUsageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponseStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiUsageLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyUsageSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TotalRequests = table.Column<int>(type: "int", nullable: false),
                    EndpointBreakdown = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyUsageSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyUsageSummaries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Tiers",
                columns: new[] { "Id", "MonthlyPriceUsd", "MonthlyQuota", "Name", "RequestsPerSecond" },
                values: new object[,]
                {
                    { 1, 0m, 1000, "Free", 2 },
                    { 2, 50m, 100000, "Pro", 10 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLogs_CustomerId",
                table: "ApiUsageLogs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ApiKeyHash",
                table: "Customers",
                column: "ApiKeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TierId",
                table: "Customers",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyUsageSummaries_CustomerId_Year_Month",
                table: "MonthlyUsageSummaries",
                columns: new[] { "CustomerId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiUsageLogs");

            migrationBuilder.DropTable(
                name: "MonthlyUsageSummaries");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Tiers");
        }
    }
}
