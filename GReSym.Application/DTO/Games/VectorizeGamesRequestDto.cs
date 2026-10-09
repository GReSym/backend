using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Games;

public class VectorizeGamesRequestDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(1000)]
    public List<int> GameIds { get; set; } = [];
}
