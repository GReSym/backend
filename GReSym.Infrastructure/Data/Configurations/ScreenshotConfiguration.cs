using GReSym.Core.Entities.GameInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class ScreenshotConfiguration : IEntityTypeConfiguration<Screenshot>
{
    public void Configure(EntityTypeBuilder<Screenshot> builder)
    {
        builder.ToTable("screenshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.GameId)
            .HasColumnName("game_id");

        builder.Property(s => s.Url)
            .HasColumnName("url")
            .HasMaxLength(255);

        builder.Property(s => s.Width)
            .HasColumnName("width");

        builder.Property(s => s.Height)
            .HasColumnName("height");

        builder.Property(s => s.Caption)
            .HasColumnName("caption")
            .HasColumnType("text");

        builder.Property(s => s.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(s => s.Game)
            .WithMany(g => g.Screenshots)
            .HasForeignKey(s => s.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.GameId)
            .HasDatabaseName("screenshots_games_FK");
    }
}