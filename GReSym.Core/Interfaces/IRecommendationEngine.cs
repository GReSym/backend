using GReSym.Core.Models;

namespace GReSym.Core.Interfaces;

/// <summary>The ML service: builds recommendations from game vectors.</summary>
public interface IRecommendationEngine
{
    /// <summary>Best games first. Empty if none of the rated games are vectorized yet.</summary>
    /// <exception cref="Exceptions.RecommendationEngineException">ML service unavailable or returned an error.</exception>
    Task<IReadOnlyList<ScoredGame>> RecommendAsync(
        IReadOnlyCollection<GameRatingSignal> ratings,
        int limit,
        CancellationToken cancellationToken = default);
}
