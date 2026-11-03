using System.Net;
using GReSym.Core.Interfaces;

namespace GReSym.Infrastructure.Media;

public sealed class HttpExternalImageSource : IExternalImageSource
{
    // Steam's largest screenshots are ~2–3 MB; anything far beyond that isn't one
    private const long MaxImageBytes = 20L * 1024 * 1024;

    private readonly HttpClient _http;

    public HttpExternalImageSource(HttpClient http)
    {
        _http = http;
    }

    public async Task<byte[]?> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new HttpRequestException($"{url} answered {(int)response.StatusCode}", null, response.StatusCode);
        if (!response.IsSuccessStatusCode)
            return null;

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType == null || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return null;
        if (response.Content.Headers.ContentLength > MaxImageBytes)
            return null;

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
