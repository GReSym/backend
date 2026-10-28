using GReSym.Core.Entities.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class SteamUserGameConfiguration : IEntityTypeConfiguration<SteamUserGame>
{
    public void Configure(EntityTypeBuilder<SteamUserGame> builder)
    {
        builder.ToTable("steam_user_games");

        builder.HasKey(g => new { g.SteamUserId, g.GameId });

        builder.Property(g => g.SteamUserId)
            .HasColumnName("steam_user_id");

        builder.Property(g => g.GameId)
            .HasColumnName("game_id");

        builder.Property(g => g.PlaytimeMinutes)
            .HasColumnName("playtime_minutes");

        builder.Property(g => g.Recommended)
            .HasColumnName("recommended")
            .HasComment("The user's Steam review: 1 up, 0 down, NULL none");

        // Relationships
        builder.HasOne(g => g.SteamUser)
            .WithMany(u => u.Games)
            .HasForeignKey(g => g.SteamUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.Game)
            .WithMany()
            .HasForeignKey(g => g.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(g => g.GameId)
            .HasDatabaseName("steam_user_games_games_FK");
    }
}
