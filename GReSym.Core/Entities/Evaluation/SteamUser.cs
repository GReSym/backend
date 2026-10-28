using GReSym.Core.Entities.Base;
using System.Collections.ObjectModel;

namespace GReSym.Core.Entities.Evaluation;

/// <summary>
/// Anonymized public Steam user collected for the offline evaluation (ml/scripts/collect_steam_users.py).
/// Not an account: no SteamID or persona name is stored.
/// </summary>
public class SteamUser : BaseEntity
{
    /// <summary>Internal name, e.g. "user-1".</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>HMAC-SHA256 of the SteamID64 (hex), only to avoid importing the same user twice.</summary>
    public string SteamIdHash { get; set; } = string.Empty;
    public int GamesOwned { get; set; }
    /// <summary>Owned games with enough playtime, including those missing from our catalogue.</summary>
    public int GamesPlayed { get; set; }
    public int ReviewsCount { get; set; }
    public DateTime? CollectedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Навигационные свойства
    public virtual ICollection<SteamUserGame> Games { get; set; } = new Collection<SteamUserGame>();
}
