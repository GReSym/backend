using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GReSym.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    mainstream = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)10),
                    dislike_mode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValue: "ignore")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dislike_strength = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)50),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_settings");
        }
    }
}
