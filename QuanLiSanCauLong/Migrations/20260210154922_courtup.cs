using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiSanCauLong.Migrations
{
    /// <inheritdoc />
    public partial class courtup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasCanteen",
                table: "Courts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasParking",
                table: "Courts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasShower",
                table: "Courts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasWifi",
                table: "Courts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PeakHourPrice",
                table: "Courts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RegularPrice",
                table: "Courts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeekendPrice",
                table: "Courts",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasCanteen",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "HasParking",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "HasShower",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "HasWifi",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "PeakHourPrice",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "RegularPrice",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "WeekendPrice",
                table: "Courts");
        }
    }
}
