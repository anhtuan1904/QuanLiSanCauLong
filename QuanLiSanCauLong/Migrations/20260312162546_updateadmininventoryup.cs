using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiSanCauLong.Migrations
{
    /// <inheritdoc />
    public partial class updateadmininventoryup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MinQuantity",
                table: "ProductVariants",
                newName: "RentedQuantity");

            migrationBuilder.AddColumn<int>(
                name: "DamagedQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CleaningFee",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LaborUnit",
                table: "Products",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MaterialPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MaxRentalHours",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDeposit",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DamagedQuantity",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "MaxQuantity",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CleaningFee",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LaborPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LaborUnit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaterialPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxRentalHours",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RequiresDeposit",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "RentedQuantity",
                table: "ProductVariants",
                newName: "MinQuantity");
        }
    }
}
