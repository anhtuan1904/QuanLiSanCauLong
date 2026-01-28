using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiSanCauLong.Migrations
{
    /// <inheritdoc />
    public partial class update1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SystemSettings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "SystemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_CreatedBy",
                table: "SystemSettings",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemSettings_Users_CreatedBy",
                table: "SystemSettings",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SystemSettings_Users_CreatedBy",
                table: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_CreatedBy",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SystemSettings");
        }
    }
}
