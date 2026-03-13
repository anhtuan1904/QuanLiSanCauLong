using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiSanCauLong.Migrations
{
    /// <inheritdoc />
    public partial class updatepriceslot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BufferMinutes",
                table: "PriceSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MemberDiscount",
                table: "PriceSlots",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MinDurationMinutes",
                table: "PriceSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "PriceSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BufferMinutes",
                table: "PriceSlots");

            migrationBuilder.DropColumn(
                name: "MemberDiscount",
                table: "PriceSlots");

            migrationBuilder.DropColumn(
                name: "MinDurationMinutes",
                table: "PriceSlots");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "PriceSlots");
        }
    }
}
