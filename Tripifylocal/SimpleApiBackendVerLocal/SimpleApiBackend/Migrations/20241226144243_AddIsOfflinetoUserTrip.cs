using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOfflinetoUserTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOffline",
                table: "UserTrips",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOffline",
                table: "UserTrips");
        }
    }
}
