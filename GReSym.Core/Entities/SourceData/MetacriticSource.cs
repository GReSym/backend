using GReSym.Core.Entities.Base;
using GReSym.Core.Entities.GameInfo;

namespace GReSym.Core.Entities.SourceData;

public class MetacriticSource : BaseEntity
{
    public int? GameId { get; set; }
    public string MetacriticUrl { get; set; } = string.Empty;
    public float? Metascore { get; set; }
    public float? UserScore { get; set; }
    public int? RatingCount { get; set; }
    public int? CriticReviewsCount { get; set; }
    public int? UserReviewsCount { get; set; }
    
    // Навигационные свойства
    public virtual Game? Game { get; set; }
}