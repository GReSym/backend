using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GReSym.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewsMeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reviews_negative",
                table: "steam_source",
                type: "int",
                nullable: true,
                comment: "Steam total_negative (all languages)");

            migrationBuilder.AddColumn<int>(
                name: "reviews_positive",
                table: "steam_source",
                type: "int",
                nullable: true,
                comment: "Steam total_positive (all languages)");

            migrationBuilder.AddColumn<int>(
                name: "playtime_at_review",
                table: "reviews",
                type: "int",
                nullable: true,
                comment: "Minutes played when the review was written");

            migrationBuilder.AddColumn<int>(
                name: "votes_funny",
                table: "reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "votes_up",
                table: "reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "weighted_vote_score",
                table: "reviews",
                type: "double",
                nullable: true,
                comment: "Steam helpfulness score, 0..1");

            migrationBuilder.AddColumn<bool>(
                name: "written_during_early_access",
                table: "reviews",
                type: "tinyint(1)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reviews_negative",
                table: "steam_source");

            migrationBuilder.DropColumn(
                name: "reviews_positive",
                table: "steam_source");

            migrationBuilder.DropColumn(
                name: "playtime_at_review",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "votes_funny",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "votes_up",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "weighted_vote_score",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "written_during_early_access",
                table: "reviews");
        }
    }
}
