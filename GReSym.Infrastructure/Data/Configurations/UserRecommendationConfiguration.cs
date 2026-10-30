using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class UserRecommendationConfiguration : IEntityTypeConfiguration<UserRecommendation>
{
    public void Configure(EntityTypeBuilder<UserRecommendation> builder)
    {
        builder.ToTable("user_recommendations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.UserId)
            .HasColumnName("user_id");

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(r => r.Source)
            .HasColumnName("source")
            .HasMaxLength(16);

        builder.Property(r => r.GameLimit)
            .HasColumnName("game_limit")
            .HasColumnType("tinyint unsigned")
            .HasConversion<byte>()
            .HasComment("Requested number of games; older recommendations keep fewer");

        builder.Property(r => r.Mainstream)
            .HasColumnName("mainstream")
            .HasColumnType("tinyint unsigned")
            .HasConversion<byte>();

        // The same snake_case names as user_settings
        builder.Property(r => r.DislikeMode)
            .HasColumnName("dislike_mode")
            .HasMaxLength(32)
            .HasConversion(m => m.ToName(), name => DislikeModes.Parse(name));

        builder.Property(r => r.DislikeStrength)
            .HasColumnName("dislike_strength")
            .HasColumnType("tinyint unsigned")
            .HasConversion<byte>();

        builder.Property(r => r.UsedRatings)
            .HasColumnName("used_ratings");

        builder.Property(r => r.Error)
            .HasColumnName("error")
            .HasColumnType("text");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(r => r.FinishedAt)
            .HasColumnName("finished_at");

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // History newest first; also serves the FK
        builder.HasIndex(r => new { r.UserId, r.Id })
            .HasDatabaseName("user_recommendations_user_id_IDX");

        // For deleting old recommendations
        builder.HasIndex(r => r.UpdatedAt)
            .HasDatabaseName("user_recommendations_updated_at_IDX");
    }
}
