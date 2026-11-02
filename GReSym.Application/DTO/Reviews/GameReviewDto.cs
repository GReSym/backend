using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Reviews;

/// <summary>A parsed (Steam) review of a game. Authors are not exposed.</summary>
public class GameReviewDto
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Source { get; set; } = string.Empty;
    public string? Content { get; set; }
    public bool? Recommended { get; set; }
    /// <summary>Minutes played when the review was written.</summary>
    public int? PlaytimeAtReview { get; set; }
    public int? VotesUp { get; set; }
    public bool? WrittenDuringEarlyAccess { get; set; }
    public DateTime? CreatedAt { get; set; }
}
