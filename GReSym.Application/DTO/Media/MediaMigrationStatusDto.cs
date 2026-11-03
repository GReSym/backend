using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Media;

/// <summary>The current (or last) media migration run. Kept in memory: an API restart forgets it.</summary>
public class MediaMigrationStatusDto
{
    /// <summary>idle (no run since the API started) | running | completed | cancelled | failed.</summary>
    [Required]
    public string State { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int Limit { get; set; }
    public bool IncludeScreenshots { get; set; }

    [Required]
    public int GamesProcessed { get; set; }

    /// <summary>Games with at least one image that couldn't be moved (gone from Steam, rejected); they keep the Steam URL.</summary>
    [Required]
    public int GamesFailed { get; set; }

    [Required]
    public int HeadersMigrated { get; set; }

    [Required]
    public int ScreenshotsMigrated { get; set; }

    [Required]
    public int FilesFailed { get; set; }

    /// <summary>Why the run failed (Openinary or the DB unavailable).</summary>
    public string? Error { get; set; }
}
