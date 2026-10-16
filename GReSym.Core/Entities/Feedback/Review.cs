using GReSym.Core.Entities.Base;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Enums;

namespace GReSym.Core.Entities.Feedback;

public class Review : BaseEntity
{
    public int? GameId { get; set; }
    public ReviewSource Source { get; set; }
    public string? Author { get; set; }
    public string? Content { get; set; }
    public float? Rating { get; set; }
    public bool? Recommended { get; set; }
    public string? ExternalId { get; set; }

    // Steam review metadata (used by the ML service to pick reviews for embeddings)
    public int? VotesUp { get; set; }
    public int? VotesFunny { get; set; }
    public double? WeightedVoteScore { get; set; }
    public int? PlaytimeAtReview { get; set; }  // minutes
    public bool? WrittenDuringEarlyAccess { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Навигационные свойства
    public virtual Game? Game { get; set; }
}