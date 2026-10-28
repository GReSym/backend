using GReSym.Core.Models;

namespace GReSym.Core.Interfaces;

/// <summary>The ML service: builds recommendations from game vectors.</summary>
public interface IRecommendationEngine
{
    /// <summary>Best games first. Empty if none of the liked games are vectorized yet.</summary>
    /// <param name="mainstream">0–100, how much popular games are favoured; null = the engine's default.</param>
    /// <exception cref="Exceptions.RecommendationEngineException">ML service unavailable or returned an error.</exception>
    Task<IReadOnlyList<ScoredGame>> RecommendAsync(
        IReadOnlyCollection<GameRatingSignal> ratings,
        int limit,
        int? mainstream = null,
        CancellationToken cancellationToken = default);
}
