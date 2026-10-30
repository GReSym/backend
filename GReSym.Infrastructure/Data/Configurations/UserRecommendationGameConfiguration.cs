using GReSym.Core.Entities.UserInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class UserRecommendationGameConfiguration : IEntityTypeConfiguration<UserRecommendationGame>
{
    public void Configure(EntityTypeBuilder<UserRecommendationGame> builder)
    {
        builder.ToTable("user_recommendation_games");

        builder.HasKey(g => new { g.RecommendationId, g.GameId });

        builder.Property(g => g.RecommendationId)
            .HasColumnName("recommendation_id");

        builder.Property(g => g.GameId)
            .HasColumnName("game_id");

        builder.Property(g => g.Position)
            .HasColumnName("position")
            .HasColumnType("tinyint unsigned")
            .HasConversion<byte>()
            .HasComment("1-based rank");

        builder.Property(g => g.Score)
            .HasColumnName("score")
            .HasComment("ML score; null for popular games");

        builder.Property(g => g.Liked)
            .HasColumnName("liked")
            .HasComment("The user's feedback; null = none");

        builder.HasOne(g => g.Recommendation)
            .WithMany(r => r.Games)
            .HasForeignKey(g => g.RecommendationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.Game)
            .WithMany()
            .HasForeignKey(g => g.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => g.GameId)
            .HasDatabaseName("user_recommendation_games_game_id_IDX");
    }
}
