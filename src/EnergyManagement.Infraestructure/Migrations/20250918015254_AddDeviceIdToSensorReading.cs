using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyManagement.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceIdToSensorReading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SensorReadings_Devices_DeviceId1",
                table: "SensorReadings");

            migrationBuilder.DropIndex(
                name: "IX_SensorReadings_DeviceId1",
                table: "SensorReadings");

            migrationBuilder.DropColumn(
                name: "DeviceId1",
                table: "SensorReadings");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceId",
                table: "SensorReadings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_DeviceId_Timestamp",
                table: "SensorReadings",
                columns: new[] { "DeviceId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SensorReadings_DeviceId_Timestamp",
                table: "SensorReadings");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceId",
                table: "SensorReadings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceId1",
                table: "SensorReadings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_DeviceId1",
                table: "SensorReadings",
                column: "DeviceId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SensorReadings_Devices_DeviceId1",
                table: "SensorReadings",
                column: "DeviceId1",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
