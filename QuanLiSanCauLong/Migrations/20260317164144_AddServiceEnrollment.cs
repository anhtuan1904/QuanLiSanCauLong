using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiSanCauLong.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Skills",
                table: "JobApplications",
                newName: "TechnicalSkills");

            migrationBuilder.RenameColumn(
                name: "Education",
                table: "JobApplications",
                newName: "Major");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "JobPostings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "JobPostings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUrgent",
                table: "JobPostings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "JobPostings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalaryType",
                table: "JobPostings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentPosition",
                table: "JobApplications",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalInfo",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CVFileName",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Certificates",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "JobApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentCompany",
                table: "JobApplications",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "JobApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "JobApplications",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GraduationRank",
                table: "JobApplications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GraduationYear",
                table: "JobApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewDate",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewNote",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInUrl",
                table: "JobApplications",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortfolioUrl",
                table: "JobApplications",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralSource",
                table: "JobApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoftSkills",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "University",
                table: "JobApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceEnrollments",
                columns: table => new
                {
                    EnrollmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    StringingId = table.Column<int>(type: "int", nullable: true),
                    TournamentId = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceEnrollments", x => x.EnrollmentId);
                    table.ForeignKey(
                        name: "FK_ServiceEnrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId");
                    table.ForeignKey(
                        name: "FK_ServiceEnrollments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceEnrollments_StringingServices_StringingId",
                        column: x => x.StringingId,
                        principalTable: "StringingServices",
                        principalColumn: "StringingId");
                    table.ForeignKey(
                        name: "FK_ServiceEnrollments_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "TournamentId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceEnrollments_CourseId",
                table: "ServiceEnrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceEnrollments_OrderId",
                table: "ServiceEnrollments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceEnrollments_StringingId",
                table: "ServiceEnrollments",
                column: "StringingId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceEnrollments_TournamentId",
                table: "ServiceEnrollments",
                column: "TournamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceEnrollments");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "IsUrgent",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "SalaryType",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "AdditionalInfo",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "CVFileName",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "Certificates",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "City",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "CurrentCompany",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "GraduationRank",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "GraduationYear",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewDate",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewNote",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "Languages",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "LinkedInUrl",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "PortfolioUrl",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ReferralSource",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "SoftSkills",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "University",
                table: "JobApplications");

            migrationBuilder.RenameColumn(
                name: "TechnicalSkills",
                table: "JobApplications",
                newName: "Skills");

            migrationBuilder.RenameColumn(
                name: "Major",
                table: "JobApplications",
                newName: "Education");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "JobPostings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentPosition",
                table: "JobApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);
        }
    }
}
