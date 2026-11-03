using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class UserGameListEntryConfiguration : IEntityTypeConfiguration<UserGameListEntry>
{
    public void Configure(EntityTypeBuilder<UserGameListEntry> builder)
    {
        builder.ToTable("user_game_lists");

        builder.HasKey(e => new { e.UserId, e.GameId });

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever();

        builder.Property(e => e.GameId)
            .HasColumnName("game_id")
            .ValueGeneratedNever();

        // Stored as the readable name ("planned" / "rejected"), the same one the API uses
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(16)
            .HasConversion(s => s.ToName(), name => GameListStatuses.Parse(name))
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Game)
            .WithMany()
            .HasForeignKey(e => e.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // One list page of a user, newest first
        builder.HasIndex(e => new { e.UserId, e.Status, e.UpdatedAt })
            .HasDatabaseName("IX_user_game_lists_user_status_updated");

        builder.HasIndex(e => e.GameId)
            .HasDatabaseName("IX_user_game_lists_game_id");
    }
}
