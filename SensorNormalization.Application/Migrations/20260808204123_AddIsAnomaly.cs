using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SensorNormalization.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAnomaly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnomaly",
                table: "sensor_readings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAnomaly",
                table: "sensor_readings");
        }
    }
}
