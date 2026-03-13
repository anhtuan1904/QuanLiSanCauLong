using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiSanCauLong.Migrations
{
    /// <inheritdoc />
    public partial class update_productcategory1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowCustomerMaterial",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPartialReturn",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BehaviorType",
                table: "ProductCategories",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ChargeOvertime",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultCleaningFee",
                table: "ProductCategories",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultDepositAmount",
                table: "ProductCategories",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "DepositRequired",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaterialUnit",
                table: "ProductCategories",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxRentalHours",
                table: "ProductCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PricingModel",
                table: "ProductCategories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresSize",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SeparateLaborAndMaterial",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowCustomerMaterial",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "AllowPartialReturn",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "BehaviorType",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "ChargeOvertime",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DefaultCleaningFee",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DefaultDepositAmount",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DepositRequired",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "MaterialUnit",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "MaxRentalHours",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "PricingModel",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "RequiresSize",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "SeparateLaborAndMaterial",
                table: "ProductCategories");
        }
    }
}
