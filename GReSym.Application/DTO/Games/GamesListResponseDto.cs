using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Games;

public class GamesListResponseDto
{
    [Required]
    public int Count { get; set; }
    [Required]
    public List<GameInfoResponseDto> Games { get; set; } = [];
}