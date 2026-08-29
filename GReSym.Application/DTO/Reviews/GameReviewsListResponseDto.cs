using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Reviews;

public class GameReviewsListDto
{
    [Required]
    public int Count { get; set; }
    [Required]
    public List<GameReviewDto> reviews = [];
}