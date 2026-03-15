using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiSanCauLong.Migrations
{
    /// <inheritdoc />
    public partial class courtreivew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewLikes_Users_UserId",
                table: "ReviewLikes");

            migrationBuilder.DropIndex(
                name: "IX_ReviewLikes_ReviewId",
                table: "ReviewLikes");

            migrationBuilder.AddColumn<int>(
                name: "CourtReviewReviewId",
                table: "CourtReviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLikes_ReviewId_UserId",
                table: "ReviewLikes",
                columns: new[] { "ReviewId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourtReviews_CourtReviewReviewId",
                table: "CourtReviews",
                column: "CourtReviewReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourtReviews_CourtReviews_CourtReviewReviewId",
                table: "CourtReviews",
                column: "CourtReviewReviewId",
                principalTable: "CourtReviews",
                principalColumn: "ReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewLikes_Users_UserId",
                table: "ReviewLikes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourtReviews_CourtReviews_CourtReviewReviewId",
                table: "CourtReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ReviewLikes_Users_UserId",
                table: "ReviewLikes");

            migrationBuilder.DropIndex(
                name: "IX_ReviewLikes_ReviewId_UserId",
                table: "ReviewLikes");

            migrationBuilder.DropIndex(
                name: "IX_CourtReviews_CourtReviewReviewId",
                table: "CourtReviews");

            migrationBuilder.DropColumn(
                name: "CourtReviewReviewId",
                table: "CourtReviews");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLikes_ReviewId",
                table: "ReviewLikes",
                column: "ReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewLikes_Users_UserId",
                table: "ReviewLikes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
