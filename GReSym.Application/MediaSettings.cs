namespace GReSym.Application;

/// <summary>"Media" section of appsettings: self-hosted images (Openinary behind the media gateway, deploy/media/).</summary>
public class MediaSettings
{
    /// <summary>Media base URL until the admin sets one (app_settings "media.base_url"). Empty = not configured.</summary>
    public string DefaultBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Variants rendered right after upload, so the first visitor gets them from the cache. The same strings as
    /// deploy/media/presets.conf and web-frontend/src/utils/media.ts.
    /// </summary>
    public List<string> HeaderPresets { get; set; } = ["w_460,f_webp"];
    public List<string> ScreenshotPresets { get; set; } = ["w_600,h_338,c_fill,f_webp", "f_webp,q_90"];

    /// <summary>Games migrated in parallel (each: download from Steam, upload, prewarm).</summary>
    public int MigrationConcurrency { get; set; } = 4;
    public int DownloadTimeoutSeconds { get; set; } = 30;
}
