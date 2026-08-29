using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Games;

public class GameRequestDto
{
    [Required]
    public int GameId { get; set; }
}