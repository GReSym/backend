namespace GReSym.Core.Entities.Settings;

/// <summary>
/// Site-wide setting changed by the admin at runtime (key/value). Settings that need a restart anyway stay in appsettings.
/// </summary>
public class AppSetting
{
    public const int MaxKeyLength = 64;

    /// <summary>Base URL of the media CDN (Openinary gateway) that stored image paths are relative to.</summary>
    public const string MediaBaseUrl = "media.base_url";

    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
