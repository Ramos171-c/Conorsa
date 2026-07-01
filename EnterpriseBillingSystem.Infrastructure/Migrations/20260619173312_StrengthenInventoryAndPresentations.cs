using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseBillingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenInventoryAndPresentations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductUnitConversions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Inventory_AvailableStock",
                table: "Inventory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Inventory_PhysicalStock",
                table: "Inventory");

            migrationBuilder.DropColumn(
                name: "QuantityPerPresentation",
                table: "ProductPresentations");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductPresentationId",
                table: "SalesInvoiceDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductPresentationId",
                table: "PurchaseInvoiceDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "AllowPurchase",
                table: "ProductPresentations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowSale",
                table: "ProductPresentations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductPresentationId",
                table: "InventoryMovementDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "AllowNegativeInventory",
                table: "BranchWarehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceDetails_ProductPresentationId",
                table: "SalesInvoiceDetails",
                column: "ProductPresentationId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceDetails_ProductPresentationId",
                table: "PurchaseInvoiceDetails",
                column: "ProductPresentationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPresentations_ProductId_Name",
                table: "ProductPresentations",
                columns: new[] { "ProductId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovementDetails_ProductPresentationId",
                table: "InventoryMovementDetails",
                column: "ProductPresentationId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovementDetails_ProductPresentations_ProductPresentationId",
                table: "InventoryMovementDetails",
                column: "ProductPresentationId",
                principalTable: "ProductPresentations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoiceDetails_ProductPresentations_ProductPresentationId",
                table: "PurchaseInvoiceDetails",
                column: "ProductPresentationId",
                principalTable: "ProductPresentations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceDetails_ProductPresentations_ProductPresentationId",
                table: "SalesInvoiceDetails",
                column: "ProductPresentationId",
                principalTable: "ProductPresentations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovementDetails_ProductPresentations_ProductPresentationId",
                table: "InventoryMovementDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoiceDetails_ProductPresentations_ProductPresentationId",
                table: "PurchaseInvoiceDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceDetails_ProductPresentations_ProductPresentationId",
                table: "SalesInvoiceDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceDetails_ProductPresentationId",
                table: "SalesInvoiceDetails");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoiceDetails_ProductPresentationId",
                table: "PurchaseInvoiceDetails");

            migrationBuilder.DropIndex(
                name: "IX_ProductPresentations_ProductId_Name",
                table: "ProductPresentations");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovementDetails_ProductPresentationId",
                table: "InventoryMovementDetails");

            migrationBuilder.DropColumn(
                name: "ProductPresentationId",
                table: "SalesInvoiceDetails");

            migrationBuilder.DropColumn(
                name: "ProductPresentationId",
                table: "PurchaseInvoiceDetails");

            migrationBuilder.DropColumn(
                name: "AllowPurchase",
                table: "ProductPresentations");

            migrationBuilder.DropColumn(
                name: "AllowSale",
                table: "ProductPresentations");

            migrationBuilder.DropColumn(
                name: "ProductPresentationId",
                table: "InventoryMovementDetails");

            migrationBuilder.DropColumn(
                name: "AllowNegativeInventory",
                table: "BranchWarehouses");

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerPresentation",
                table: "ProductPresentations",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.CreateTable(
                name: "ProductUnitConversions",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnitConversions", x => new { x.ProductId, x.FromUnitId, x.ToUnitId });
                    table.CheckConstraint("CK_ProductUnitConversions_ConversionFactor", "[ConversionFactor] > 0.000000");
                    table.ForeignKey(
                        name: "FK_ProductUnitConversions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductUnitConversions_UnitsOfMeasure_FromUnitId",
                        column: x => x.FromUnitId,
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductUnitConversions_UnitsOfMeasure_ToUnitId",
                        column: x => x.ToUnitId,
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Inventory_AvailableStock",
                table: "Inventory",
                sql: "([PhysicalStock] - [ReservedStock] - [CommittedStock]) >= 0.0000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Inventory_PhysicalStock",
                table: "Inventory",
                sql: "[PhysicalStock] >= 0.0000");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitConversions_FromUnitId",
                table: "ProductUnitConversions",
                column: "FromUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitConversions_ToUnitId",
                table: "ProductUnitConversions",
                column: "ToUnitId");
        }
    }
}
