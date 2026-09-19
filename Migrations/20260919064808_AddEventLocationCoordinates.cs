using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NavGuru.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLocationCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "SupportResources",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "SupportResources",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "SupportResources");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "SupportResources");
        }
    }
}
