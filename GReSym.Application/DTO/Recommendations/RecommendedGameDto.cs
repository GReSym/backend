using System.ComponentModel.DataAnnotations;
using GReSym.Application.DTO.Games;

namespace GReSym.Application.DTO.Recommendations;

public class RecommendedGameDto : GameInfoResponseDto
{
    /// <summary>1-based rank in the recommendation.</summary>
    [Required]
    public int Position { get; set; }

    /// <summary>ML score (kNN similarity + popularity term). Null for popular-games fallback.</summary>
    public double? Score { get; set; }

    /// <summary>The user's feedback on this recommendation; null = none.</summary>
    public bool? Liked { get; set; }
}
