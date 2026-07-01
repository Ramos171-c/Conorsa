using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseBillingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerTypeToSalesInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerType",
                table: "SalesInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Natural");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerType",
                table: "SalesInvoices");
        }
    }
}
