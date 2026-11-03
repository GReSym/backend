using GReSym.Core.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("app_settings");

        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key)
            .HasColumnName("key")
            .HasMaxLength(AppSetting.MaxKeyLength)
            .ValueGeneratedNever();

        builder.Property(s => s.Value)
            .HasColumnName("value")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
