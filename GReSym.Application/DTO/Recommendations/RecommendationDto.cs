using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Recommendations;

/// <summary>One recommendation of the history, with its games (newest first in the list).</summary>
public class RecommendationDto
{
    [Required]
    public int Id { get; set; }

    /// <summary>queued or completed.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// "personal" (from the user's ratings) or "popular" (fallback: no usable ratings, nothing vectorized yet, or ML/broker
    /// unavailable). Null until completed.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>The requested number of games. Only the newest recommendation keeps all of them.</summary>
    [Required]
    public int Limit { get; set; }

    [Required]
    public int Mainstream { get; set; }
    [Required]
    public string DislikeMode { get; set; } = string.Empty;
    [Required]
    public int DislikeStrength { get; set; }

    public int? UsedRatings { get; set; }
    public string? Error { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }
    [Required]
    public DateTime UpdatedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    /// <summary>Number of games stored (in the history list) or returned.</summary>
    [Required]
    public int Count { get; set; }

    /// <summary>Best first. Only for a single recommendation.</summary>
    public List<RecommendedGameDto>? Games { get; set; }
}

public class RecommendationHistoryDto
{
    /// <summary>Items on this page.</summary>
    [Required]
    public int Count { get; set; }
    [Required]
    public List<RecommendationDto> Recommendations { get; set; } = [];
}

public class CreateRecommendationResponseDto
{
    [Required]
    public int Id { get; set; }

    /// <summary>queued, or completed when popular games were filled in right away.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class RecommendationFeedbackRequestDto
{
    /// <summary>true = good recommendation, false = bad one, null = clear the feedback.</summary>
    public bool? Liked { get; set; }
}
