using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Settings;

/// <summary>Site settings the admin can change at runtime.</summary>
public class AppSettingsDto
{
    /// <summary>The effective media base URL (see <see cref="PublicConfigDto.MediaBaseUrl"/>).</summary>
    [Required]
    public string MediaBaseUrl { get; set; } = string.Empty;

    /// <summary>Media base URL from appsettings, used while <see cref="MediaBaseUrl"/> was never set.</summary>
    [Required]
    public string DefaultMediaBaseUrl { get; set; } = string.Empty;
}
