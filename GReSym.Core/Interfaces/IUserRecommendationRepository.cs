using GReSym.Core.Entities.UserInfo;

namespace GReSym.Core.Interfaces;

/// <summary>Users' recommendation history. Changes are saved by <see cref="SaveChangesAsync"/>.</summary>
public interface IUserRecommendationRepository
{
    Task AddAsync(UserRecommendation recommendation);

    /// <summary>Tracked, without games.</summary>
    Task<UserRecommendation?> GetAsync(int id);

    /// <summary>With its games ordered by position, no tracking.</summary>
    Task<UserRecommendation?> GetWithGamesAsync(int id);

    /// <summary>The user's recommendations, newest first, without games.</summary>
    Task<List<(UserRecommendation Recommendation, int GameCount)>> GetHistoryAsync(int userId, int page, int pageSize);

    /// <summary>Tracked row, null if the game isn't (or no longer is) in that recommendation.</summary>
    Task<UserRecommendationGame?> GetGameAsync(int recommendationId, int gameId);

    Task<bool> HasNewerCompletedAsync(int userId, int recommendationId);

    /// <summary>
    /// Deletes the games past <paramref name="keep"/> positions without feedback from the user's completed recommendations
    /// older than <paramref name="recommendationId"/>, and bumps updated_at of the ones that changed. Runs immediately.
    /// </summary>
    /// <returns>Rows deleted.</returns>
    Task<int> CapOlderAsync(int userId, int recommendationId, int keep, CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="action"/> and <see cref="SaveChangesAsync"/> in one transaction.</summary>
    Task InTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
