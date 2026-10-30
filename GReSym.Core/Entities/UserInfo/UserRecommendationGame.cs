using GReSym.Core.Entities.GameInfo;

namespace GReSym.Core.Entities.UserInfo;

/// <summary>A game of a <see cref="UserRecommendation"/> and the user's opinion about recommending it.</summary>
public class UserRecommendationGame
{
    public int RecommendationId { get; set; }
    public int GameId { get; set; }

    /// <summary>1-based rank in the list.</summary>
    public int Position { get; set; }

    /// <summary>The ML score (higher is better); null for popular games.</summary>
    public double? Score { get; set; }

    /// <summary>The user's feedback on this recommendation; null = none.</summary>
    public bool? Liked { get; set; }

    // Навигационные свойства
    public virtual UserRecommendation Recommendation { get; set; } = null!;
    public virtual Game Game { get; set; } = null!;
}
