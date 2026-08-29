using GReSym.Core.Entities.GameInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class GameTagConfiguration : IEntityTypeConfiguration<GameTag>
{
    public void Configure(EntityTypeBuilder<GameTag> builder)
    {
        builder.ToTable("game_tags");

        builder.HasKey(gt => new { gt.GameId, gt.TagId });

        builder.Property(gt => gt.GameId)
            .HasColumnName("game_id");

        builder.Property(gt => gt.TagId)
            .HasColumnName("tag_id");

        // Relationships
        builder.HasOne(gt => gt.Game)
            .WithMany(g => g.GameTags)
            .HasForeignKey(gt => gt.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(gt => gt.TagId)
            .HasDatabaseName("game_tags_tags_FK");
    }
}