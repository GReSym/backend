using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Recommendations;

public class RecommendationsResponseDto
{
    public const string SourcePersonal = "personal";
    public const string SourcePopular = "popular";

    /// <summary>"personal" (from the user's ratings) or "popular" (fallback: no usable ratings or ML unavailable).</summary>
    [Required]
    public string Source { get; set; } = SourcePopular;
    [Required]
    public int Count { get; set; }
    [Required]
    public List<RecommendedGameDto> Games { get; set; } = [];
}
