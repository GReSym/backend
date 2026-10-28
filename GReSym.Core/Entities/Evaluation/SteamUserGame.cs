using GReSym.Core.Entities.GameInfo;

namespace GReSym.Core.Entities.Evaluation;

/// <summary>A game of a collected Steam user that is in our catalogue.</summary>
public class SteamUserGame
{
    public int SteamUserId { get; set; }
    public int GameId { get; set; }
    public int PlaytimeMinutes { get; set; }
    /// <summary>The user's own Steam review (thumbs up/down); null without one.</summary>
    public bool? Recommended { get; set; }

    // Навигационные свойства
    public virtual SteamUser SteamUser { get; set; } = null!;
    public virtual Game Game { get; set; } = null!;
}
