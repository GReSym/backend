namespace GReSym.Core.Models;

/// <summary>Why a game's vector is out of date (ml/src/ml/staleness.py <c>Reason</c>).</summary>
public static class StaleReasons
{
    public const string Description = "description";
    public const string Sentiment = "sentiment";
    public const string Reviews = "reviews";
    public const string Growth = "growth";
    public const string Outdated = "outdated";
    public const string Missing = "missing";
    public const string Deleted = "deleted";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { Description, Sentiment, Reviews, Growth, Outdated, Missing, Deleted };
}

/// <summary>A game whose vector should be rebuilt. Higher score = more worth re-vectorizing.</summary>
public record StaleGame(
    int GameId,
    double Score,
    IReadOnlyList<string> Reasons,
    DateTime? VectorizedAt,
    IReadOnlyDictionary<string, double?> Details);

/// <param name="Counts">Stale games per reason, over all games (before the reason filter).</param>
/// <param name="Total">Games matching the reason filter (Items is one page of them).</param>
public record StaleGamesReport(
    string Collection,
    int TextVersion,
    int Vectors,
    int Games,
    IReadOnlyDictionary<string, int> Counts,
    int Total,
    IReadOnlyList<StaleGame> Items);
