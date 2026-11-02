using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Users;

/// <summary>Profile statistics computed from the user's game library (users_games_rates).</summary>
public class UserStatsDto
{
    /// <summary>Sum of player hours over the library.</summary>
    [Required]
    public long TotalHours { get; set; }

    /// <summary>Number of games in the library.</summary>
    [Required]
    public int GamesPlayed { get; set; }

    /// <summary>Most frequent gameplay tag (store features skipped), liked games count double, disliked ones not at all; null if none.</summary>
    public string? FavoriteTag { get; set; }
}
