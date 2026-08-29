using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Games;

public class GamesListRequestDto
{
    [Required]
    public int Page { get; set; } = 1;
    [Required]
    [Range(1, 100)]
    public int Limit { get; set; } = 20;

    public string? Search { get; set; }
    public string? Tags { get; set; }
}