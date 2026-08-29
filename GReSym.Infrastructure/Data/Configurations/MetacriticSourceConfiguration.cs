using GReSym.Core.Entities.SourceData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace GReSym.Infrastructure.Data.Configurations;

public class MetacriticSourceConfiguration : IEntityTypeConfiguration<MetacriticSource>
{
    public void Configure(EntityTypeBuilder<MetacriticSource> builder)
    {
        builder.ToTable("metacritic_source");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(m => m.GameId)
            .HasColumnName("game_id");

        builder.Property(m => m.MetacriticUrl)
            .HasColumnName("metacritic_url")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(m => m.Metascore)
            .HasColumnName("metascore");

        builder.Property(m => m.UserScore)
            .HasColumnName("user_score");

        builder.Property(m => m.RatingCount)
            .HasColumnName("rating_count");

        builder.Property(m => m.CriticReviewsCount)
            .HasColumnName("critic_reviews_count");

        builder.Property(m => m.UserReviewsCount)
            .HasColumnName("user_reviews_count");

        // Unique constraint
        builder.HasIndex(m => m.MetacriticUrl)
            .IsUnique()
            .HasDatabaseName("metacritic_source_unique");

        // Relationships
        builder.HasOne(m => m.Game)
            .WithOne(g => g.MetacriticSource)
            .HasForeignKey<MetacriticSource>(m => m.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(m => m.GameId)
            .HasDatabaseName("metacritic_source_games_FK");
    }
}