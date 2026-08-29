using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Reviews;

public class GameReviewsListRequestDto
{
    [Required]
    public int Page { get; set; } = 1;
    [Required]
    [Range(1, 100)]
    public int Limit { get; set; } = 20;
}