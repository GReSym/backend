namespace GReSym.Core.Enums;

/// <summary>
/// The user's list a game is in (<c>user_game_lists.status</c>). Stored and sent to the client as the lowercase names of
/// <see cref="GameListStatuses"/>.
/// </summary>
public enum GameListStatus
{
    /// <summary>The user wants to play it (liked recommendation): a weak like for the recommendations.</summary>
    Planned,

    /// <summary>The user isn't interested (disliked recommendation): a weak dislike for the recommendations.</summary>
    Rejected
}

public static class GameListStatuses
{
    private static readonly Dictionary<GameListStatus, string> Names = new()
    {
        [GameListStatus.Planned] = "planned",
        [GameListStatus.Rejected] = "rejected"
    };

    private static readonly Dictionary<string, GameListStatus> ByName =
        Names.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> AllNames => Names.Values;

    public static string ToName(this GameListStatus status) => Names[status];

    public static bool TryParse(string? name, out GameListStatus status) =>
        ByName.TryGetValue(name ?? string.Empty, out status);

    public static GameListStatus Parse(string name) =>
        TryParse(name, out var status) ? status : throw new ArgumentException($"Unknown game list status '{name}'", nameof(name));

    /// <summary>The list a recommendation feedback puts the game in: liked → planned, disliked → rejected.</summary>
    public static GameListStatus FromFeedback(bool liked) => liked ? GameListStatus.Planned : GameListStatus.Rejected;
}
