using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddDebtPaymentRequests2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DebtPaymentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DebtId = table.Column<int>(type: "int", nullable: false),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtPaymentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebtPaymentRequests_Debts_DebtId",
                        column: x => x.DebtId,
                        principalTable: "Debts",
                        principalColumn: "DebtId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DebtPaymentRequests_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DebtPaymentRequests_DebtId",
                table: "DebtPaymentRequests",
                column: "DebtId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtPaymentRequests_RequestedById",
                table: "DebtPaymentRequests",
                column: "RequestedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DebtPaymentRequests");
        }
    }
}
