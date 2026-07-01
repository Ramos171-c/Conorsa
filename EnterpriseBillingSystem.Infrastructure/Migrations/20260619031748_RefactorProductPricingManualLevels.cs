using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseBillingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProductPricingManualLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_DefaultPurchasePrice",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_DefaultSalePrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DefaultPurchasePrice",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "DefaultSalePrice",
                table: "Products",
                newName: "CurrentCost");

            migrationBuilder.RenameColumn(
                name: "OldPrice",
                table: "ProductPriceHistories",
                newName: "OldWholesalePrice");

            migrationBuilder.RenameColumn(
                name: "NewPrice",
                table: "ProductPriceHistories",
                newName: "OldSemiWholesalePrice");

            migrationBuilder.AddColumn<bool>(
                name: "AllowPromotions",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoMarkSoldOut",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogBadge",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FavoriteOrder",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HighlightInCatalog",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // 1. Add TaxId as nullable uniqueidentifier
            migrationBuilder.AddColumn<Guid>(
                name: "TaxId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            // 2. Transfer data from ProductTaxes to Products
            migrationBuilder.Sql("UPDATE P SET P.TaxId = PT.TaxId FROM Products P INNER JOIN ProductTaxes PT ON P.Id = PT.ProductId");

            // 3. Set a default Tax for any orphaned products
            migrationBuilder.Sql("UPDATE Products SET TaxId = (SELECT TOP 1 Id FROM Taxes) WHERE TaxId IS NULL");

            // 4. Alter TaxId to be non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "TaxId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: false,
                oldNullable: true);

            // 5. Drop ProductTaxes table
            migrationBuilder.DropTable(
                name: "ProductTaxes");

            migrationBuilder.AddColumn<decimal>(
                name: "NewCost",
                table: "ProductPriceHistories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NewRetailPrice",
                table: "ProductPriceHistories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NewSemiWholesalePrice",
                table: "ProductPriceHistories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NewWholesalePrice",
                table: "ProductPriceHistories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OldCost",
                table: "ProductPriceHistories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OldRetailPrice",
                table: "ProductPriceHistories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductPresentationId",
                table: "ProductPriceHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductPresentations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1m),
                    QuantityPerPresentation = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1m),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    RetailPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    SemiWholesalePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    WholesalePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    SemiWholesaleThreshold = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    WholesaleThreshold = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsBaseUnit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDefaultSalePresentation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPresentations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPresentations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductPresentations_UnitsOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TaxId",
                table: "Products",
                column: "TaxId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_CurrentCost",
                table: "Products",
                sql: "[CurrentCost] >= 0.0000");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistories_ProductPresentationId",
                table: "ProductPriceHistories",
                column: "ProductPresentationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPresentations_Barcode",
                table: "ProductPresentations",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPresentations_ProductId",
                table: "ProductPresentations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPresentations_ProductId_IsDefaultSalePresentation",
                table: "ProductPresentations",
                columns: new[] { "ProductId", "IsDefaultSalePresentation" },
                unique: true,
                filter: "[IsDefaultSalePresentation] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPresentations_UnitOfMeasureId",
                table: "ProductPresentations",
                column: "UnitOfMeasureId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPriceHistories_ProductPresentations_ProductPresentationId",
                table: "ProductPriceHistories",
                column: "ProductPresentationId",
                principalTable: "ProductPresentations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Taxes_TaxId",
                table: "Products",
                column: "TaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPriceHistories_ProductPresentations_ProductPresentationId",
                table: "ProductPriceHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Taxes_TaxId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "ProductPresentations");

            migrationBuilder.DropIndex(
                name: "IX_Products_TaxId",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_CurrentCost",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductPriceHistories_ProductPresentationId",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "AllowPromotions",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AutoMarkSoldOut",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CatalogBadge",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FavoriteOrder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HighlightInCatalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NewCost",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "NewRetailPrice",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "NewSemiWholesalePrice",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "NewWholesalePrice",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "OldCost",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "OldRetailPrice",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "ProductPresentationId",
                table: "ProductPriceHistories");

            migrationBuilder.RenameColumn(
                name: "CurrentCost",
                table: "Products",
                newName: "DefaultSalePrice");

            migrationBuilder.RenameColumn(
                name: "OldWholesalePrice",
                table: "ProductPriceHistories",
                newName: "OldPrice");

            migrationBuilder.RenameColumn(
                name: "OldSemiWholesalePrice",
                table: "ProductPriceHistories",
                newName: "NewPrice");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPurchasePrice",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ProductTaxes",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTaxes", x => new { x.ProductId, x.TaxId });
                    table.ForeignKey(
                        name: "FK_ProductTaxes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTaxes_Taxes_TaxId",
                        column: x => x.TaxId,
                        principalTable: "Taxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_DefaultPurchasePrice",
                table: "Products",
                sql: "[DefaultPurchasePrice] >= 0.0000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_DefaultSalePrice",
                table: "Products",
                sql: "[DefaultSalePrice] >= 0.0000");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTaxes_TaxId",
                table: "ProductTaxes",
                column: "TaxId");
        }
    }
}
