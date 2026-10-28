using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GReSym.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSteamEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evaluation_runs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    parameters = table.Column<string>(type: "longtext", nullable: false, comment: "JSON, read by the ML worker")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    results = table.Column<string>(type: "longtext", nullable: true, comment: "JSON report from the ML worker")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    error = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    users_evaluated = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    finished_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_runs", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "steam_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    steam_id_hash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false, comment: "HMAC-SHA256 of the SteamID64 (hex), for deduplication only")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    games_owned = table.Column<int>(type: "int", nullable: false),
                    games_played = table.Column<int>(type: "int", nullable: false, comment: "Owned games with enough playtime, including those missing from the catalogue"),
                    reviews_count = table.Column<int>(type: "int", nullable: false),
                    collected_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_steam_users", x => x.id);
                },
                comment: "Anonymized public Steam users for the offline evaluation")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "steam_user_games",
                columns: table => new
                {
                    steam_user_id = table.Column<int>(type: "int", nullable: false),
                    game_id = table.Column<int>(type: "int", nullable: false),
                    playtime_minutes = table.Column<int>(type: "int", nullable: false),
                    recommended = table.Column<bool>(type: "tinyint(1)", nullable: true, comment: "The user's Steam review: 1 up, 0 down, NULL none")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_steam_user_games", x => new { x.steam_user_id, x.game_id });
                    table.ForeignKey(
                        name: "FK_steam_user_games_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_steam_user_games_steam_users_steam_user_id",
                        column: x => x.steam_user_id,
                        principalTable: "steam_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "steam_user_games_games_FK",
                table: "steam_user_games",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "steam_users_hash_unique",
                table: "steam_users",
                column: "steam_id_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "steam_users_name_unique",
                table: "steam_users",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evaluation_runs");

            migrationBuilder.DropTable(
                name: "steam_user_games");

            migrationBuilder.DropTable(
                name: "steam_users");
        }
    }
}
