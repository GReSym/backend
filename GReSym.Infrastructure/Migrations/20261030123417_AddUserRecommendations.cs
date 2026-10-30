using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GReSym.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_recommendations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    game_limit = table.Column<byte>(type: "tinyint unsigned", nullable: false, comment: "Requested number of games; older recommendations keep fewer"),
                    mainstream = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    dislike_mode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dislike_strength = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    used_ratings = table.Column<int>(type: "int", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    finished_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_recommendations", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_recommendations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_recommendation_games",
                columns: table => new
                {
                    recommendation_id = table.Column<int>(type: "int", nullable: false),
                    game_id = table.Column<int>(type: "int", nullable: false),
                    position = table.Column<byte>(type: "tinyint unsigned", nullable: false, comment: "1-based rank"),
                    score = table.Column<double>(type: "double", nullable: true, comment: "ML score; null for popular games"),
                    liked = table.Column<bool>(type: "tinyint(1)", nullable: true, comment: "The user's feedback; null = none")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_recommendation_games", x => new { x.recommendation_id, x.game_id });
                    table.ForeignKey(
                        name: "FK_user_recommendation_games_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_recommendation_games_user_recommendations_recommendatio~",
                        column: x => x.recommendation_id,
                        principalTable: "user_recommendations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "user_recommendation_games_game_id_IDX",
                table: "user_recommendation_games",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "user_recommendations_updated_at_IDX",
                table: "user_recommendations",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "user_recommendations_user_id_IDX",
                table: "user_recommendations",
                columns: new[] { "user_id", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_recommendation_games");

            migrationBuilder.DropTable(
                name: "user_recommendations");
        }
    }
}
