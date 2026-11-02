using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Games;

public class PopularGamesListRequestDto
{
    [Required]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int Limit { get; set; } = 20;

    public string? Tags { get; set; }
}
