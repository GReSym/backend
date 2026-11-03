using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GReSym.Infrastructure.Media;

/// <summary>
/// <see cref="IMediaStore"/> on Openinary v0.5 (POST /upload, DELETE /storage/{path}). Openinary renames an upload whose
/// name is taken ("header (1).jpg") instead of overwriting it; such a file is deleted with the old one and uploaded again.
/// </summary>
public sealed class OpeninaryMediaStore : IMediaStore
{
    private readonly HttpClient _http;
    private readonly ILogger<OpeninaryMediaStore> _logger;

    public OpeninaryMediaStore(HttpClient http, IOptions<OpeninarySettings> settings, ILogger<OpeninaryMediaStore> logger)
    {
        _http = http;
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(settings.Value.ApiKey) && !settings.Value.ApiKey.StartsWith('#'))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.Value.ApiKey);
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> UploadAsync(string folder, IReadOnlyList<MediaFile> files,
        IReadOnlyCollection<string> prewarm, CancellationToken cancellationToken = default)
    {
        if (_http.DefaultRequestHeaders.Authorization == null)
            throw new MediaStoreException("Openinary API key is not configured (openinary_api_key in backend/config.json).");
        if (files.Count == 0)
            return new Dictionary<string, string>();

        // Upload first and clean up only on a collision: a new image costs one request (the API key is rate-limited
        // per request unless lifted, see deploy/manage.sh openinary-unlimit-keys)
        var stored = new Dictionary<string, string>();
        var replace = new List<MediaFile>();
        foreach (var file in await PostAsync(folder, files, prewarm, cancellationToken))
        {
            var expected = $"{folder}/{file.Filename}";
            if (file.Path == expected)
            {
                stored[file.Filename] = file.Path;
                continue;
            }

            // The name was taken (a re-upload): drop the renamed copy and the old file, then upload again
            await DeleteAsync(file.Path, cancellationToken);
            await DeleteAsync(expected, cancellationToken);
            replace.AddRange(files.Where(f => f.Name == file.Filename));
        }

        if (replace.Count > 0)
        {
            foreach (var file in await PostAsync(folder, replace, prewarm, cancellationToken))
            {
                var expected = $"{folder}/{file.Filename}";
                if (file.Path != expected)
                {
                    // Deleted above, so only a concurrent upload of the same name can cause this
                    throw new MediaStoreException($"Openinary stored {expected} as {file.Path}.");
                }
                stored[file.Filename] = file.Path;
            }
        }
        return stored;
    }

    private async Task<List<UploadedFile>> PostAsync(string folder, IReadOnlyList<MediaFile> files,
        IReadOnlyCollection<string> prewarm, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        AddField(form, "folder", folder);
        foreach (var file in files)
        {
            var content = new ByteArrayContent(file.Content);
            content.Headers.ContentType = new MediaTypeHeaderValue(ContentType(file.Name));
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = Quote("files"),
                FileName = Quote(file.Name)
            };
            form.Add(content);
            AddField(form, "names", file.Name);
        }
        foreach (var transformation in prewarm)
        {
            AddField(form, "transformations", transformation);
        }

        UploadResponse? response;
        try
        {
            using var result = await _http.PostAsync("upload", form, cancellationToken);
            ThrowIfUnauthorized(result);
            // 200 = all stored, 207 = some files rejected, 400 = none stored (all files invalid)
            if (result.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.MultiStatus or HttpStatusCode.BadRequest))
            {
                var body = await result.Content.ReadAsStringAsync(cancellationToken);
                throw new MediaStoreException($"Openinary upload failed: {(int)result.StatusCode} {Truncate(body)}");
            }
            response = await result.Content.ReadFromJsonAsync<UploadResponse>(cancellationToken);
        }
        catch (Exception e) when (e is HttpRequestException or System.Text.Json.JsonException
                                      or TaskCanceledException { InnerException: TimeoutException })
        {
            throw new MediaStoreException("Openinary is unavailable.", e);
        }

        foreach (var error in response?.Errors ?? [])
        {
            _logger.LogWarning("Openinary rejected {Folder}/{File}: {Error}", folder, error.Filename, error.Error);
        }
        return response?.Files ?? [];
    }

    private async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        var encoded = string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
        try
        {
            using var result = await _http.DeleteAsync($"storage/{encoded}", cancellationToken);
            ThrowIfUnauthorized(result);
            if (!result.IsSuccessStatusCode && result.StatusCode != HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Openinary: deleting {Path} answered {Status}", path, (int)result.StatusCode);
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException { InnerException: TimeoutException })
        {
            throw new MediaStoreException("Openinary is unavailable.", e);
        }
    }

    // Openinary answers 401 both for a wrong key and for a key over Better Auth's per-key limit (100 requests/min,
    // reset only after a minute without requests)
    private static void ThrowIfUnauthorized(HttpResponseMessage result)
    {
        if (result.StatusCode == HttpStatusCode.Unauthorized)
            throw new MediaStoreException("Openinary rejected the API key: wrong openinary_api_key, or the key hit its " +
                                          "rate limit (lift it with deploy/manage.sh openinary-unlimit-keys).");
    }

    // MultipartFormDataContent.Add(content, name) writes `name=folder` unquoted (and adds filename*=utf-8''…).
    // Openinary parses the body with undici's FormData, which rejects unquoted names ("Failed to parse body as
    // FormData"), so every part gets an explicit, quoted Content-Disposition
    private static void AddField(MultipartFormDataContent form, string name, string value)
    {
        var content = new StringContent(value);
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = Quote(name) };
        form.Add(content);
    }

    // File and field names here are ASCII without quotes (games/{id}/header.jpg, preset strings)
    private static string Quote(string value) => $"\"{value}\"";

    private static string ContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "image/jpeg"
    };

    private static string Truncate(string text) => text.Length <= 300 ? text : text[..300];

    private sealed record UploadResponse(bool Success, List<UploadedFile>? Files, List<UploadError>? Errors);

    private sealed record UploadedFile(string Filename, string Path);

    private sealed record UploadError(string Filename, string Error);
}
