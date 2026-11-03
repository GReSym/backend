using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Settings;

/// <summary>Partial update: omitted fields keep their current value.</summary>
public class UpdateAppSettingsRequestDto
{
    /// <summary>http(s) URL or a path starting with "/"; a trailing slash is dropped. Empty = back to the default.</summary>
    [MaxLength(255)]
    public string? MediaBaseUrl { get; set; }
}
