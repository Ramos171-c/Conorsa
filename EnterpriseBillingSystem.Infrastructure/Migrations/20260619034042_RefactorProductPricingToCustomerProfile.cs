using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseBillingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProductPricingToCustomerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerPricingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPricingProfiles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CustomerPricingProfiles",
                columns: new[] { "Id", "Name", "Type", "IsActive", "IsDeleted" },
                values: new object[,]
                {
                    { new Guid("9f67a29e-c852-4752-bf6d-74d445479601"), "Detalle", 0, true, false },
                    { new Guid("8b6ea5ef-3b10-449e-b9ef-dc722956cf02"), "Semi Mayorista", 1, true, false },
                    { new Guid("f4cbe5db-4fae-4d51-a9f4-18c6422bcf03"), "Mayorista", 2, true, false }
                });

            migrationBuilder.DropColumn(
                name: "SemiWholesaleThreshold",
                table: "ProductPresentations");

            migrationBuilder.DropColumn(
                name: "WholesaleThreshold",
                table: "ProductPresentations");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerPricingProfileId",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("9f67a29e-c852-4752-bf6d-74d445479601"));

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerPricingProfileId",
                table: "Customers",
                column: "CustomerPricingProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_CustomerPricingProfiles_CustomerPricingProfileId",
                table: "Customers",
                column: "CustomerPricingProfileId",
                principalTable: "CustomerPricingProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_CustomerPricingProfiles_CustomerPricingProfileId",
                table: "Customers");

            migrationBuilder.DropTable(
                name: "CustomerPricingProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerPricingProfileId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerPricingProfileId",
                table: "Customers");

            migrationBuilder.AddColumn<int>(
                name: "SemiWholesaleThreshold",
                table: "ProductPresentations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WholesaleThreshold",
                table: "ProductPresentations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
