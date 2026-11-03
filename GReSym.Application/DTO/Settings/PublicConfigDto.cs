using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Settings;

/// <summary>Site configuration every client needs before showing anything.</summary>
public class PublicConfigDto
{
    /// <summary>
    /// Base URL of the media CDN that stored image paths (<c>games/10/header.jpg</c>) are relative to, without a trailing
    /// slash; absolute (<c>https://example.com/cdn</c>) or root-relative (<c>/cdn</c>). Empty = not configured. Image
    /// references that are absolute URLs (Steam CDN) are used as they are.
    /// </summary>
    [Required]
    public string MediaBaseUrl { get; set; } = string.Empty;
}
