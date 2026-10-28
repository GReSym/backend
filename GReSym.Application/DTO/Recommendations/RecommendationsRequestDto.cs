using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Recommendations;

public class RecommendationsRequestDto
{
    [Range(1, 100)]
    public int Limit { get; set; } = 20;

    /// <summary>
    /// How mainstream the recommendations are: 0 = only by similarity to the games you like (the most niche games),
    /// 100 = strongly favours popular games. Omitted = the ML service's default (10).
    /// </summary>
    [Range(0, 100)]
    public int? Mainstream { get; set; }
}
