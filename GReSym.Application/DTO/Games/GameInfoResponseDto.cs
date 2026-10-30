using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Games;

public class GameInfoResponseDto
{
    [Required]
    public int GameId { get; set; }
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public DateOnly ReleaseDate { get; set; }
    [Required]
    public string Developer { get; set; } = string.Empty;
    [Required]
    public string Publisher { get; set; } = string.Empty;
    [Required]
    public string? HeaderImageUrl { get; set; }

    // Optional details
    /// <summary>HTML. Only for a single game (GET /api/games/{id}, admin PATCH); null in lists.</summary>
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Screenshots { get; set; }
    public int? SteamAppId { get; set; }

}
