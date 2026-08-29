using GReSym.Core.Entities.SourceData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace GReSym.Infrastructure.Data.Configurations;

public class SteamSourceConfiguration : IEntityTypeConfiguration<SteamSource>
{
    public void Configure(EntityTypeBuilder<SteamSource> builder)
    {
        builder.ToTable("steam_source");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.GameId)
            .HasColumnName("game_id");

        builder.Property(s => s.SteamAppId)
            .HasColumnName("steam_app_id")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(s => s.Price)
            .HasColumnName("price")
            .HasPrecision(10, 2);

        builder.Property(s => s.RecommendationsCount)
            .HasColumnName("recommendations_count");

        builder.Property(s => s.ReleaseDate)
            .HasColumnName("release_date")
            .HasColumnType("date");

        builder.Property(s => s.IsFree)
            .HasColumnName("is_free");

        // Unique constraint
        builder.HasIndex(s => s.SteamAppId)
            .IsUnique()
            .HasDatabaseName("steam_source_unique");

        // Relationships
        builder.HasOne(s => s.Game)
            .WithOne(g => g.SteamSource)
            .HasForeignKey<SteamSource>(s => s.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.GameId)
            .HasDatabaseName("steam_source_games_FK");
    }
}