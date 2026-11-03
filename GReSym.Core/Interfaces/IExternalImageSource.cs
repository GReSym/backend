namespace GReSym.Core.Interfaces;

/// <summary>Downloads images still referenced by absolute URL (Steam CDN) for the media migration.</summary>
public interface IExternalImageSource
{
    /// <summary>The image's bytes, null if it's gone (4xx) or isn't an image.</summary>
    /// <exception cref="HttpRequestException">Network failure or a 5xx answer.</exception>
    Task<byte[]?> DownloadAsync(string url, CancellationToken cancellationToken = default);
}
