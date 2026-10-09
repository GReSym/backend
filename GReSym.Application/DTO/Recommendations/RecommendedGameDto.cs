using GReSym.Application.DTO.Games;

namespace GReSym.Application.DTO.Recommendations;

public class RecommendedGameDto : GameInfoResponseDto
{
    /// <summary>Cosine similarity to the user's taste profile. Null for popular-games fallback.</summary>
    public double? Score { get; set; }
}
