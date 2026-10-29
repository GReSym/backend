using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings");

        builder.HasKey(s => s.UserId);

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever();

        // DB defaults for rows inserted by hand. The sentinels make EF always send the entity's value: with the CLR default (0)
        // as the sentinel, a user's 0 / Ignore would be left out of the INSERT and replaced by the DB default.
        builder.Property(s => s.Mainstream)
            .HasColumnName("mainstream")
            .HasColumnType("tinyint unsigned")
            .HasConversion<byte>()
            .HasDefaultValue(UserSettings.DefaultMainstream)
            .HasSentinel(-1);

        // Stored as the readable snake_case name ("hide_similar"), the same one the API and the ML service use
        builder.Property(s => s.DislikeMode)
            .HasColumnName("dislike_mode")
            .HasMaxLength(32)
            .HasConversion(m => m.ToName(), name => DislikeModes.Parse(name))
            .HasDefaultValue(UserSettings.DefaultDislikeMode)
            .HasSentinel((DislikeMode)(-1));

        builder.Property(s => s.DislikeStrength)
            .HasColumnName("dislike_strength")
            .HasColumnType("tinyint unsigned")
            .HasConversion<byte>()
            .HasDefaultValue(UserSettings.DefaultDislikeStrength)
            .HasSentinel(-1);

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(s => s.User)
            .WithOne(u => u.Settings)
            .HasForeignKey<UserSettings>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
