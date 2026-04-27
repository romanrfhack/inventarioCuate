using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RefaccionariaCuate.Infrastructure.Persistence;

#nullable disable

namespace RefaccionariaCuate.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260427234000_RepairSupplierCatalogSchema")]
    public partial class RepairSupplierCatalogSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                schema: "app",
                table: "InitialInventoryLoadDetails",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                schema: "app",
                table: "InitialInventoryLoadDetails",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "app",
                table: "InitialInventoryLoadDetails",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                schema: "app",
                table: "InitialInventoryLoadDetails",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                schema: "app",
                table: "InitialInventoryLoadDetails",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "app",
                table: "Products",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Compatibility",
                schema: "app",
                table: "Products",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Family",
                schema: "app",
                table: "Products",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Line",
                schema: "app",
                table: "Products",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PiecesPerBox",
                schema: "app",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubFamily",
                schema: "app",
                table: "Products",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SupplierAvailability",
                schema: "app",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SupplierCatalogUpdatedAt",
                schema: "app",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                schema: "app",
                table: "Products",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierStockText",
                schema: "app",
                table: "Products",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupplierCatalogImportBatches",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ImportProfile = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmationToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCatalogImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierCatalogImportDetails",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierCatalogImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceRow = table.Column<int>(type: "int", nullable: false),
                    SourceSheet = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ImportProfile = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SupplierProductCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PiecesPerBox = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Compatibility = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Line = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Family = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SubFamily = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SuggestedSalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PriceLevelsJson = table.Column<string>(type: "TEXT", nullable: true),
                    SupplierAvailability = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SupplierStockText = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RequiresRevision = table.Column<bool>(type: "bit", nullable: false),
                    RevisionReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    MatchType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RowStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MatchedProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ProposedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProposedSalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ApplySelected = table.Column<bool>(type: "bit", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCatalogImportDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierCatalogImportDetails_SupplierCatalogImportBatches_SupplierCatalogImportBatchId",
                        column: x => x.SupplierCatalogImportBatchId,
                        principalSchema: "app",
                        principalTable: "SupplierCatalogImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCatalogImportDetails_SupplierCatalogImportBatchId_SourceRow",
                schema: "app",
                table: "SupplierCatalogImportDetails",
                columns: new[] { "SupplierCatalogImportBatchId", "SourceRow" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierCatalogImportDetails",
                schema: "app");

            migrationBuilder.DropTable(
                name: "SupplierCatalogImportBatches",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "Brand",
                schema: "app",
                table: "InitialInventoryLoadDetails");

            migrationBuilder.DropColumn(
                name: "Location",
                schema: "app",
                table: "InitialInventoryLoadDetails");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "app",
                table: "InitialInventoryLoadDetails");

            migrationBuilder.DropColumn(
                name: "Supplier",
                schema: "app",
                table: "InitialInventoryLoadDetails");

            migrationBuilder.DropColumn(
                name: "Unit",
                schema: "app",
                table: "InitialInventoryLoadDetails");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "app",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Compatibility",
                schema: "app",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Family",
                schema: "app",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Line",
                schema: "app",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PiecesPerBox",
                schema: "app",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SubFamily",
                schema: "app",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupplierAvailability",
                schema: "app",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupplierCatalogUpdatedAt",
                schema: "app",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                schema: "app",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupplierStockText",
                schema: "app",
                table: "Products");
        }
    }
}
