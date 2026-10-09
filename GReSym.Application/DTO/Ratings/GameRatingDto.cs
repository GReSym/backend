using System.ComponentModel.DataAnnotations;
using GReSym.Application.DTO.Games;

namespace GReSym.Application.DTO.Ratings;

public class GameRatingDto
{
    [Required]
    public int GameId { get; set; }
    public int? Rating { get; set; }
    public bool? Recommend { get; set; }
    public int? PlayerHours { get; set; }
    public string? Comment { get; set; }

    // Only in lists
    public GameInfoResponseDto? Game { get; set; }
}
