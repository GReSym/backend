using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Enums;

namespace GReSym.Core.Entities.UserInfo;

/// <summary>
/// A game the user marked as planned or rejected (from a recommendation's feedback or the game page). Listed games are never
/// recommended again and act as weak likes / dislikes. A game leaves the list when the user rates it in the library.
/// </summary>
public class UserGameListEntry
{
    public int UserId { get; set; }
    public int GameId { get; set; }
    public GameListStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>When the game was put in its current list (the lists are ordered by it, newest first).</summary>
    public DateTime UpdatedAt { get; set; }

    // Навигационные свойства
    public virtual User User { get; set; } = null!;
    public virtual Game Game { get; set; } = null!;
}
