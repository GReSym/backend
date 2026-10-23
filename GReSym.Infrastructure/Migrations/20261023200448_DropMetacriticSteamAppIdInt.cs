using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GReSym.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropMetacriticSteamAppIdInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metacritic_source");

            migrationBuilder.AlterColumn<int>(
                name: "steam_app_id",
                table: "steam_source",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "reviews",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                comment: "'steam', 'internal', 'etc.'",
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldComment: "'steam', 'metacritic', 'etc.'")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "steam_app_id",
                table: "steam_source",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "reviews",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                comment: "'steam', 'metacritic', 'etc.'",
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldComment: "'steam', 'internal', 'etc.'")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "metacritic_source",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    game_id = table.Column<int>(type: "int", nullable: true),
                    critic_reviews_count = table.Column<int>(type: "int", nullable: true),
                    metacritic_url = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    metascore = table.Column<float>(type: "float", nullable: true),
                    rating_count = table.Column<int>(type: "int", nullable: true),
                    user_reviews_count = table.Column<int>(type: "int", nullable: true),
                    user_score = table.Column<float>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metacritic_source", x => x.id);
                    table.ForeignKey(
                        name: "FK_metacritic_source_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "metacritic_source_games_FK",
                table: "metacritic_source",
                column: "game_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "metacritic_source_unique",
                table: "metacritic_source",
                column: "metacritic_url",
                unique: true);
        }
    }
}
