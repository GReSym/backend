using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Ratings;

public class GameRatingsListResponseDto
{
    [Required]
    public int Count { get; set; }
    [Required]
    public List<GameRatingDto> Ratings { get; set; } = [];
}
