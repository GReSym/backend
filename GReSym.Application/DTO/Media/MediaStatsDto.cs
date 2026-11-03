using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Media;

/// <summary>How many image references still point at the Steam CDN.</summary>
public class MediaStatsDto
{
    [Required]
    public int Headers { get; set; }

    [Required]
    public int SteamHeaders { get; set; }

    [Required]
    public int Screenshots { get; set; }

    [Required]
    public int SteamScreenshots { get; set; }
}
