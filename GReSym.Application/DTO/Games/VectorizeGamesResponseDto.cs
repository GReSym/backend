using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Games;

public class VectorizeGamesResponseDto
{
    [Required]
    public int Queued { get; set; }
}
