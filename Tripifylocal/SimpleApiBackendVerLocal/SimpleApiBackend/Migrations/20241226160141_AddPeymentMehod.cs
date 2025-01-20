using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPeymentMehod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "DebtPaymentRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "DebtPaymentRequests");
        }
    }
}
