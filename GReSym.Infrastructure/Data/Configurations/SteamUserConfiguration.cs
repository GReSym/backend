using GReSym.Core.Entities.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class SteamUserConfiguration : IEntityTypeConfiguration<SteamUser>
{
    public void Configure(EntityTypeBuilder<SteamUser> builder)
    {
        builder.ToTable("steam_users", t => t.HasComment("Anonymized public Steam users for the offline evaluation"));

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(u => u.SteamIdHash)
            .HasColumnName("steam_id_hash")
            .IsRequired()
            .HasMaxLength(64)
            .IsFixedLength()
            .HasComment("HMAC-SHA256 of the SteamID64 (hex), for deduplication only");

        builder.Property(u => u.GamesOwned)
            .HasColumnName("games_owned");

        builder.Property(u => u.GamesPlayed)
            .HasColumnName("games_played")
            .HasComment("Owned games with enough playtime, including those missing from the catalogue");

        builder.Property(u => u.ReviewsCount)
            .HasColumnName("reviews_count");

        builder.Property(u => u.CollectedAt)
            .HasColumnName("collected_at");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Unique constraints
        builder.HasIndex(u => u.Name)
            .IsUnique()
            .HasDatabaseName("steam_users_name_unique");

        builder.HasIndex(u => u.SteamIdHash)
            .IsUnique()
            .HasDatabaseName("steam_users_hash_unique");
    }
}
