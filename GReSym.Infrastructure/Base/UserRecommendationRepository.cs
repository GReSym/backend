using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Infrastructure.Base;

public class UserRecommendationRepository : IUserRecommendationRepository
{
    private readonly ApplicationDbContext _context;

    public UserRecommendationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserRecommendation recommendation)
    {
        await _context.UserRecommendations.AddAsync(recommendation);
    }

    public async Task<UserRecommendation?> GetAsync(int id)
    {
        return await _context.UserRecommendations.FindAsync(id);
    }

    public async Task<UserRecommendation?> GetWithGamesAsync(int id)
    {
        return await _context.UserRecommendations
            .AsNoTracking()
            .Include(r => r.Games.OrderBy(g => g.Position))
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<(UserRecommendation Recommendation, int GameCount)>> GetHistoryAsync(int userId, int page,
        int pageSize)
    {
        var rows = await _context.UserRecommendations
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { Recommendation = r, GameCount = r.Games.Count })
            .ToListAsync();

        return rows.Select(r => (r.Recommendation, r.GameCount)).ToList();
    }

    public async Task<UserRecommendationGame?> GetGameAsync(int recommendationId, int gameId)
    {
        return await _context.UserRecommendationGames.FindAsync(recommendationId, gameId);
    }

    public async Task<bool> HasNewerCompletedAsync(int userId, int recommendationId)
    {
        return await _context.UserRecommendations
            .AnyAsync(r => r.UserId == userId && r.Id > recommendationId && r.Status == RecommendationStatus.Completed);
    }

    public async Task<int> CapOlderAsync(int userId, int recommendationId, int keep,
        CancellationToken cancellationToken = default)
    {
        var changedIds = await _context.UserRecommendationGames
            .Where(g => g.Recommendation.UserId == userId
                        && g.RecommendationId < recommendationId
                        && g.Recommendation.Status == RecommendationStatus.Completed
                        && g.Position > keep
                        && g.Liked == null)
            .Select(g => g.RecommendationId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (changedIds.Count == 0)
            return 0;

        // By ids, so the DELETE needs no join with user_recommendations
        var deleted = await _context.UserRecommendationGames
            .Where(g => changedIds.Contains(g.RecommendationId) && g.Position > keep && g.Liked == null)
            .ExecuteDeleteAsync(cancellationToken);

        var now = DateTime.UtcNow;
        await _context.UserRecommendations
            .Where(r => changedIds.Contains(r.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.UpdatedAt, now), cancellationToken);

        return deleted;
    }

    public async Task InTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await action();
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
