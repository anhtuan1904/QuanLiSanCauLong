using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiSanCauLong.Migrations
{
    /// <inheritdoc />
    public partial class upmodelsuserbookingcourt1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricePerHour",
                table: "Bookings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerHour",
                table: "Bookings");
        }
    }
}
