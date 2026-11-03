using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Media;

public class StartMediaMigrationRequestDto
{
    /// <summary>Only these games. Omitted = every game with Steam-hosted images, most popular first.</summary>
    [MaxLength(10000)]
    public List<int>? GameIds { get; set; }

    /// <summary>At most this many games in this run.</summary>
    [Range(1, 200_000)]
    public int Limit { get; set; } = 1000;

    /// <summary>
    /// Also the screenshots (~7 per game, 150–450 KB each: ~300 GB for the whole catalogue). Without it only headers move.
    /// </summary>
    public bool IncludeScreenshots { get; set; }
}
