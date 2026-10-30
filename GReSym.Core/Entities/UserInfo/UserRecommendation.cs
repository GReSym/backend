using System.Collections.ObjectModel;
using GReSym.Core.Entities.Base;
using GReSym.Core.Enums;

namespace GReSym.Core.Entities.UserInfo;

/// <summary>
/// One recommendation request of a user and its result (history). Computed by the ML worker, or filled with popular games
/// right away when there's nothing to go on. Only the newest one keeps all <see cref="GameLimit"/> games, older ones are capped.
/// <see cref="AuditableEntity.UpdatedAt"/> changes with the result, feedback and capping.
/// </summary>
public class UserRecommendation : AuditableEntity
{
    public int UserId { get; set; }

    /// <summary>One of <see cref="RecommendationStatus"/>.</summary>
    public string Status { get; set; } = RecommendationStatus.Queued;

    /// <summary>One of <see cref="RecommendationSource"/>; null until completed.</summary>
    public string? Source { get; set; }

    /// <summary>The requested number of games.</summary>
    public int GameLimit { get; set; }

    // The tuning the list was built with (stored settings + request overrides)
    public int Mainstream { get; set; }
    public DislikeMode DislikeMode { get; set; }
    public int DislikeStrength { get; set; }

    /// <summary>Ratings the ML worker actually used (see ml.md); null for popular games.</summary>
    public int? UsedRatings { get; set; }

    /// <summary>Why popular games were used instead of personal ones (ML worker failure, broker down); null otherwise.</summary>
    public string? Error { get; set; }
    public DateTime? FinishedAt { get; set; }

    // Навигационные свойства
    public virtual User User { get; set; } = null!;
    public virtual ICollection<UserRecommendationGame> Games { get; set; } = new Collection<UserRecommendationGame>();
}

/// <summary>Values of <see cref="UserRecommendation.Status"/>.</summary>
public static class RecommendationStatus
{
    public const string Queued = "queued";
    /// <summary>Final: personal games, or popular ones when the ML worker had nothing to go on or failed.</summary>
    public const string Completed = "completed";
}

/// <summary>Values of <see cref="UserRecommendation.Source"/>.</summary>
public static class RecommendationSource
{
    /// <summary>From the user's ratings (ML worker).</summary>
    public const string Personal = "personal";
    /// <summary>Fallback: no usable ratings, nothing vectorized, ML failed or the broker was down.</summary>
    public const string Popular = "popular";
}
