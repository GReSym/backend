using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Reviews;

public class GameReviewsListDto
{
    /// <summary>Number of reviews on this page.</summary>
    [Required]
    public int Count { get; set; }
    [Required]
    public List<GameReviewDto> Reviews { get; set; } = [];
}
