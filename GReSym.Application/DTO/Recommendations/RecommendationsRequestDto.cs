using System.ComponentModel.DataAnnotations;
using GReSym.Application.Validation;

namespace GReSym.Application.DTO.Recommendations;

/// <summary>
/// The tuning fields override the user's stored settings (<c>/api/users/me/settings</c>) for this request only,
/// e.g. to preview a slider before saving it. Omitted = the stored value.
/// </summary>
public class RecommendationsRequestDto
{
    [Range(1, 100)]
    public int Limit { get; set; } = 20;

    /// <summary>
    /// How mainstream the recommendations are: 0 = only by similarity to the games you like (the most niche games),
    /// 100 = strongly favours popular games.
    /// </summary>
    [Range(0, 100)]
    public int? Mainstream { get; set; }

    /// <summary>ignore | penalize_closer | penalize_similar | hide_similar.</summary>
    [DislikeModeName]
    public string? DislikeMode { get; set; }

    [Range(0, 100)]
    public int? DislikeStrength { get; set; }
}
