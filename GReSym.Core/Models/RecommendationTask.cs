namespace GReSym.Core.Models;

/// <summary>
/// What the ML worker needs to compute a recommendation: a snapshot of the user's ratings, planned / rejected games (weak likes /
/// dislikes, never recommended; games that are also rated are left out) and the tuning.
/// </summary>
public record RecommendationTask(
    int RecommendationId,
    IReadOnlyCollection<GameRatingSignal> Ratings,
    IReadOnlyCollection<int> Planned,
    IReadOnlyCollection<int> Rejected,
    int Limit,
    RecommendationTuning Tuning);

/// <summary>
/// The ML worker's answer. Completed with no items = nothing to go on (none of the liked games vectorized, only dislikes).
/// </summary>
public record RecommendationResult(
    int RecommendationId,
    bool Completed,
    IReadOnlyList<ScoredGame> Items,
    int? UsedRatings,
    string? Error,
    DateTime At);
