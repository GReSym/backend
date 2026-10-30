using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Interfaces;
using GReSym.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GReSym.Application.Services;

/// <summary>
/// Completes recommendations: saves the ML worker's games, or popular games the user hasn't rated as the fallback,
/// and caps the user's older recommendations to <see cref="RecommendationSettings.HistoryGamesLimit"/> games.
/// </summary>
public class RecommendationResultHandler : IRecommendationResultHandler
{
    private readonly IUserRecommendationRepository _recommendations;
    private readonly IUserGameRateRepository _rateRepository;
    private readonly IGameRepository _gameRepository;
    private readonly RecommendationSettings _settings;
    private readonly ILogger<RecommendationResultHandler> _logger;

    public RecommendationResultHandler(
        IUserRecommendationRepository recommendations,
        IUserGameRateRepository rateRepository,
        IGameRepository gameRepository,
        IOptions<RecommendationSettings> settings,
        ILogger<RecommendationResultHandler> logger)
    {
        _recommendations = recommendations;
        _rateRepository = rateRepository;
        _gameRepository = gameRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task HandleAsync(RecommendationResult result, CancellationToken cancellationToken = default)
    {
        var recommendation = await _recommendations.GetAsync(result.RecommendationId);
        if (recommendation is null)
        {
            _logger.LogWarning("Result for unknown recommendation {RecommendationId}, dropped", result.RecommendationId);
            return;
        }

        // Redelivered result
        if (recommendation.Status == RecommendationStatus.Completed)
            return;

        if (!result.Completed)
        {
            _logger.LogWarning("ML worker failed recommendation {RecommendationId} ({Error}), falling back to popular games",
                recommendation.Id, result.Error);
            await CompletePopularAsync(recommendation, $"ML worker failed: {result.Error}", cancellationToken);
            return;
        }

        // Skip games deleted from MariaDB but still present in Milvus
        var existing = (await _gameRepository.GetByIdsAsync(result.Items.Select(i => i.GameId).ToList()))
            .Select(g => g.Id)
            .ToHashSet();
        var games = result.Items
            .Where(i => existing.Contains(i.GameId))
            .Select(i => (i.GameId, (double?)i.Score))
            .ToList();

        // Empty when none of the liked games are vectorized yet, or the user only disliked games
        if (games.Count == 0)
        {
            await CompletePopularAsync(recommendation, null, cancellationToken);
            return;
        }

        await CompleteAsync(recommendation, RecommendationSource.Personal, games, result.UsedRatings, null,
            result.At, cancellationToken);
        _logger.LogInformation("Recommendation {RecommendationId} completed: {Count} games", recommendation.Id,
            games.Count);
    }

    /// <summary>Completes a tracked recommendation with popular games the user hasn't rated.</summary>
    /// <param name="error">Why the personal games aren't available, if it's a failure.</param>
    public async Task CompletePopularAsync(UserRecommendation recommendation, string? error,
        CancellationToken cancellationToken = default)
    {
        var ratedIds = (await _rateRepository.GetAllByUserAsync(recommendation.UserId)).Select(r => r.GameId).ToList();
        var popular = await _gameRepository.GetPopularGamesAsync(1, recommendation.GameLimit, excludeIds: ratedIds);
        var games = popular.Select(g => (g.Id, (double?)null)).ToList();

        await CompleteAsync(recommendation, RecommendationSource.Popular, games, null, error, DateTime.UtcNow,
            cancellationToken);
    }

    /// <summary>
    /// The newest recommendation (by id, not by completion time) keeps all its games; when it completes, older ones are
    /// capped. A recommendation that completes after a newer one is capped right away. Games with feedback are never removed.
    /// </summary>
    private async Task CompleteAsync(UserRecommendation recommendation, string source,
        List<(int GameId, double? Score)> games, int? usedRatings, string? error, DateTime finishedAt,
        CancellationToken cancellationToken)
    {
        var keep = _settings.HistoryGamesLimit;

        await _recommendations.InTransactionAsync(async () =>
        {
            var isNewest = !await _recommendations.HasNewerCompletedAsync(recommendation.UserId, recommendation.Id);
            var stored = isNewest ? games : games.Take(keep).ToList();

            recommendation.Status = RecommendationStatus.Completed;
            recommendation.Source = source;
            recommendation.UsedRatings = usedRatings;
            recommendation.Error = error;
            recommendation.FinishedAt = finishedAt;
            for (var i = 0; i < stored.Count; i++)
            {
                recommendation.Games.Add(new UserRecommendationGame
                {
                    GameId = stored[i].GameId,
                    Position = i + 1,
                    Score = stored[i].Score,
                });
            }

            if (isNewest)
            {
                var removed = await _recommendations.CapOlderAsync(recommendation.UserId, recommendation.Id, keep,
                    cancellationToken);
                if (removed > 0)
                    _logger.LogDebug("Capped older recommendations of user {UserId}: {Removed} games removed",
                        recommendation.UserId, removed);
            }
        }, cancellationToken);
    }
}
