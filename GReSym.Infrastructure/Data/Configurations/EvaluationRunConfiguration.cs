using GReSym.Core.Entities.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GReSym.Infrastructure.Data.Configurations;

public class EvaluationRunConfiguration : IEntityTypeConfiguration<EvaluationRun>
{
    public void Configure(EntityTypeBuilder<EvaluationRun> builder)
    {
        builder.ToTable("evaluation_runs");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(r => r.Parameters)
            .HasColumnName("parameters")
            .IsRequired()
            .HasColumnType("longtext")
            .HasComment("JSON, read by the ML worker");

        builder.Property(r => r.Results)
            .HasColumnName("results")
            .HasColumnType("longtext")
            .HasComment("JSON report from the ML worker");

        builder.Property(r => r.Error)
            .HasColumnName("error")
            .HasColumnType("text");

        builder.Property(r => r.UsersEvaluated)
            .HasColumnName("users_evaluated");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.StartedAt)
            .HasColumnName("started_at");

        builder.Property(r => r.FinishedAt)
            .HasColumnName("finished_at");
    }
}
