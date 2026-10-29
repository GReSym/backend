using GReSym.Core.Enums;

namespace GReSym.Core.Entities.UserInfo;

/// <summary>
/// The user's recommendation tuning, 1:1 with <see cref="User"/>. A user without a row gets the defaults.
/// </summary>
public class UserSettings
{
    public const int DefaultMainstream = 10;
    public const DislikeMode DefaultDislikeMode = DislikeMode.Ignore;
    public const int DefaultDislikeStrength = 50;

    public int UserId { get; set; }

    /// <summary>0–100: 0 = only similarity to the liked games (the most niche), 100 = strongly favours popular games.</summary>
    public int Mainstream { get; set; } = DefaultMainstream;

    public DislikeMode DislikeMode { get; set; } = DefaultDislikeMode;

    /// <summary>0–100, 0 = no effect: penalty weight (penalize modes) or nearest games hidden per dislike (hide).</summary>
    public int DislikeStrength { get; set; } = DefaultDislikeStrength;

    public DateTime UpdatedAt { get; set; }

    // Навигационные свойства
    public virtual User User { get; set; } = null!;
}
