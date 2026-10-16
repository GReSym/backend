using GReSym.Core.Entities.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.GameId)
            .HasColumnName("game_id");

        builder.Property(r => r.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(64)
            .HasComment("'steam', 'metacritic', 'etc.'");

        builder.Property(r => r.Author)
            .HasColumnName("author")
            .HasMaxLength(50);

        builder.Property(r => r.Content)
            .HasColumnName("content")
            .HasColumnType("text");

        builder.Property(r => r.Rating)
            .HasColumnName("rating");

        builder.Property(r => r.Recommended)
            .HasColumnName("recommended");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(r => r.ExternalId)
            .HasColumnName("external_id");

        builder.Property(r => r.VotesUp)
            .HasColumnName("votes_up");

        builder.Property(r => r.VotesFunny)
            .HasColumnName("votes_funny");

        builder.Property(r => r.WeightedVoteScore)
            .HasColumnName("weighted_vote_score")
            .HasComment("Steam helpfulness score, 0..1");

        builder.Property(r => r.PlaytimeAtReview)
            .HasColumnName("playtime_at_review")
            .HasComment("Minutes played when the review was written");

        builder.Property(r => r.WrittenDuringEarlyAccess)
            .HasColumnName("written_during_early_access");

        // Relationships
        builder.HasOne(r => r.Game)
            .WithMany(g => g.Reviews)
            .HasForeignKey(r => r.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(r => r.GameId)
            .HasDatabaseName("reviews_games_FK");

        builder.HasIndex(r => r.Source)
            .HasDatabaseName("IX_reviews_source");

        builder.HasIndex(r => r.Rating)
            .HasDatabaseName("IX_reviews_rating");
        
        // Unique constraint
        builder.HasIndex(r => r.ExternalId)
            .IsUnique()
            .HasDatabaseName("reviews_external_id_unique");
    }
}