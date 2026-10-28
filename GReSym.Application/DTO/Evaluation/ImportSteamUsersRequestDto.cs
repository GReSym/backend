using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Evaluation;

/// <summary>The file written by ml/scripts/collect_steam_users.py (other fields of it are ignored).</summary>
public class ImportSteamUsersRequestDto
{
    [Required]
    public int Version { get; set; }

    public DateTime? CollectedAt { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(5000)]
    public List<SteamUserImportDto> Users { get; set; } = [];
}

public class SteamUserImportDto
{
    [Required]
    [MaxLength(32)]
    public string Name { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 of the SteamID64, lowercase hex.</summary>
    [Required]
    [RegularExpression("^[0-9a-f]{64}$")]
    public string SteamIdHash { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int GamesOwned { get; set; }

    [Range(0, int.MaxValue)]
    public int GamesPlayed { get; set; }

    [Range(0, int.MaxValue)]
    public int ReviewsCount { get; set; }

    [Required]
    public List<SteamUserGameImportDto> Games { get; set; } = [];
}

public class SteamUserGameImportDto
{
    [Required]
    public int SteamAppId { get; set; }

    [Range(0, int.MaxValue)]
    public int PlaytimeMinutes { get; set; }

    /// <summary>The user's review: true up, false down, null none.</summary>
    public bool? Recommended { get; set; }
}
