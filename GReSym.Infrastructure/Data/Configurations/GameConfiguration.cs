using GReSym.Core.Entities.SourceData;
using GReSym.Core.Entities.GameInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("games");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(g => g.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(g => g.Description)
            .HasColumnName("description")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(g => g.ReleaseDate)
            .HasColumnName("release_date")
            .IsRequired()
            .HasColumnType("date");

        builder.Property(g => g.Developer)
            .HasColumnName("developer")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(g => g.Publisher)
            .HasColumnName("publisher")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(g => g.HeaderImageUrl)
            .HasColumnName("header_image_url")
            .HasMaxLength(255);

        builder.Property(g => g.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(g => g.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasMany(g => g.Screenshots)
            .WithOne(s => s.Game)
            .HasForeignKey(s => s.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Reviews)
            .WithOne(r => r.Game)
            .HasForeignKey(r => r.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.UserGameRates)
            .WithOne(ugr => ugr.Game)
            .HasForeignKey(ugr => ugr.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.SteamSource)
            .WithOne(s => s.Game)
            .HasForeignKey<SteamSource>(s => s.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.MetacriticSource)
            .WithOne(m => m.Game)
            .HasForeignKey<MetacriticSource>(m => m.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.GameTags)
            .WithOne(gt => gt.Game)
            .OnDelete(DeleteBehavior.Cascade);


        builder.Ignore(g => g.Tags);

        // Indexes
        builder.HasIndex(g => g.Title)
            .HasDatabaseName("IX_games_title");

        builder.HasIndex(g => g.ReleaseDate)
            .HasDatabaseName("IX_games_release_date");
    }
}