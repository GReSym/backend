using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GReSym.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserGameLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_game_lists",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    game_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_game_lists", x => new { x.user_id, x.game_id });
                    table.ForeignKey(
                        name: "FK_user_game_lists_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_game_lists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_user_game_lists_game_id",
                table: "user_game_lists",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_game_lists_user_status_updated",
                table: "user_game_lists",
                columns: new[] { "user_id", "status", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_game_lists");
        }
    }
}
