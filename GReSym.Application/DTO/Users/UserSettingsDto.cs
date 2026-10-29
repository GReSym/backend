using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Users;

/// <summary>The user's recommendation settings (defaults if they never changed them).</summary>
public class UserSettingsDto
{
    /// <summary>0 = only similarity to the liked games (the most niche), 100 = strongly favours popular games.</summary>
    [Required]
    public int Mainstream { get; set; }

    /// <summary>ignore | penalize_closer | penalize_similar | hide_similar.</summary>
    [Required]
    public string DislikeMode { get; set; } = string.Empty;

    /// <summary>0–100, 0 = no effect. Unused when the mode is ignore.</summary>
    [Required]
    public int DislikeStrength { get; set; }
}
