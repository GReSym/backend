using GReSym.Core.Entities.Base;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Entities.Feedback;
using GReSym.Core.Entities.SourceData;
using GReSym.Core.Entities.UserInfo;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GReSym.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Games
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Screenshot> Screenshots => Set<Screenshot>();
    public DbSet<GameTag> GameTags => Set<GameTag>();

    // // Users
    public DbSet<User> Users => Set<User>();
    public DbSet<UserGameRate> UserGameRates => Set<UserGameRate>();

    // Reviews
    public DbSet<Review> Reviews => Set<Review>();

    // Source Data
    public DbSet<SteamSource> SteamSources => Set<SteamSource>();
    public DbSet<MetacriticSource> MetacriticSources => Set<MetacriticSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Применяем все конфигурации из сборки
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Автоматическое заполнение дат для AuditableEntity
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is AuditableEntity && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (AuditableEntity)entityEntry.Entity;
            var now = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
                entity.UpdatedAt = now;
            }
            else
            {
                entity.UpdatedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}