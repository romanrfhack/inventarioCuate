using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RefaccionariaCuate.Infrastructure.Persistence.Migrations
{
    public partial class AddProductSupplierCatalogSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductSupplierCatalogSnapshots",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SupplierProfile = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SupplierCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SupplierDescription = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SupplierBrand = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SupplierCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SuggestedSalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PriceLevelsJson = table.Column<string>(type: "TEXT", nullable: true),
                    SupplierAvailability = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SupplierStockText = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Compatibility = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Line = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Family = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SubFamily = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    ReviewReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSupplierCatalogSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSupplierCatalogSnapshots_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "app",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSupplierCatalogSnapshots_ProductId_SupplierProfile",
                schema: "app",
                table: "ProductSupplierCatalogSnapshots",
                columns: new[] { "ProductId", "SupplierProfile" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSupplierCatalogSnapshots",
                schema: "app");
        }
    }
}
