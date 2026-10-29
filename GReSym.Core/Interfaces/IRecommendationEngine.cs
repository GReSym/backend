using GReSym.Core.Models;

namespace GReSym.Core.Interfaces;

/// <summary>The ML service: builds recommendations from game vectors.</summary>
public interface IRecommendationEngine
{
    /// <summary>Best games first. Empty if none of the liked games are vectorized yet.</summary>
    /// <param name="tuning">The user's knobs: mainstream, dislike mode and strength.</param>
    /// <exception cref="Exceptions.RecommendationEngineException">ML service unavailable or returned an error.</exception>
    Task<IReadOnlyList<ScoredGame>> RecommendAsync(
        IReadOnlyCollection<GameRatingSignal> ratings,
        int limit,
        RecommendationTuning tuning,
        CancellationToken cancellationToken = default);
}
