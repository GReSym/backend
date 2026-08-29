using GReSym.Core.Entities.UserInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class UserGameRateConfiguration : IEntityTypeConfiguration<UserGameRate>
{
    public void Configure(EntityTypeBuilder<UserGameRate> builder)
    {
        builder.ToTable("users_games_rates");

        builder.HasKey(ugr => new { ugr.UserId, ugr.GameId });

        builder.Property(ugr => ugr.UserId)
            .HasColumnName("user_id");

        builder.Property(ugr => ugr.GameId)
            .HasColumnName("game_id");

        builder.Property(ugr => ugr.Rating)
            .HasColumnName("rating");

        builder.Property(ugr => ugr.Comment)
            .HasColumnName("comment")
            .HasColumnType("text");

        builder.Property(ugr => ugr.PlayerHours)
            .HasColumnName("player_hours");

        builder.Property(ugr => ugr.Recommend)
            .HasColumnName("recommend");

        // Relationships
        builder.HasOne(ugr => ugr.User)
            .WithMany(u => u.UserGameRates)
            .HasForeignKey(ugr => ugr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ugr => ugr.Game)
            .WithMany(g => g.UserGameRates)
            .HasForeignKey(ugr => ugr.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ugr => ugr.GameId)
            .HasDatabaseName("users_games_rates_games_FK");
    }
}