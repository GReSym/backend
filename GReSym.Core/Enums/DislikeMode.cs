namespace GReSym.Core.Enums;

/// <summary>
/// How the user's disliked games affect their recommendations (<c>user_settings.dislike_mode</c>).
/// Stored and sent to the ML service as the snake_case names of <see cref="DislikeModes"/>.
/// </summary>
public enum DislikeMode
{
    /// <summary>Dislikes don't affect recommendations (default).</summary>
    Ignore,

    /// <summary>Rank lower the games that are more like a disliked game than like any liked one (ML mode <c>margin</c>).</summary>
    PenalizeCloser,

    /// <summary>Rank lower any game similar to a disliked game (ML mode <c>knn</c>).</summary>
    PenalizeSimilar,

    /// <summary>Never recommend the nearest games of the disliked ones (ML mode <c>exclude</c>).</summary>
    HideSimilar
}

public static class DislikeModes
{
    private static readonly Dictionary<DislikeMode, string> Names = new()
    {
        [DislikeMode.Ignore] = "ignore",
        [DislikeMode.PenalizeCloser] = "penalize_closer",
        [DislikeMode.PenalizeSimilar] = "penalize_similar",
        [DislikeMode.HideSimilar] = "hide_similar"
    };

    private static readonly Dictionary<string, DislikeMode> ByName =
        Names.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> AllNames => Names.Values;

    public static string ToName(this DislikeMode mode) => Names[mode];

    public static bool TryParse(string? name, out DislikeMode mode) =>
        ByName.TryGetValue(name ?? string.Empty, out mode);

    public static DislikeMode Parse(string name) =>
        TryParse(name, out var mode) ? mode : throw new ArgumentException($"Unknown dislike mode '{name}'", nameof(name));
}
