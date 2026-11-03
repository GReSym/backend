namespace GReSym.Core.Common;

/// <summary>
/// Image references in <c>games.header_image_url</c> / <c>screenshots.url</c> are either absolute Steam CDN URLs
/// (as parsed) or paths in the media store relative to the media base URL (after migration): <c>games/10/header.jpg</c>.
/// </summary>
public static class MediaPaths
{
    /// <summary>An absolute URL (Steam CDN), not yet in the media store.</summary>
    public static bool IsExternal(string? reference) =>
        reference != null && (reference.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                              || reference.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    public static string GameFolder(int gameId) => $"games/{gameId}";

    public static string HeaderFileName(string sourceUrl) => "header" + Extension(sourceUrl);

    public static string ScreenshotFileName(int screenshotId, string sourceUrl) => $"ss-{screenshotId}" + Extension(sourceUrl);

    /// <summary>Extension of the URL's file (query string ignored), ".jpg" when it has none or an odd one.</summary>
    private static string Extension(string url)
    {
        var path = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" ? extension : ".jpg";
    }
}
