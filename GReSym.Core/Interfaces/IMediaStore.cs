namespace GReSym.Core.Interfaces;

/// <summary>Self-hosted image storage (Openinary). Paths are relative to the media base URL, e.g. <c>games/10/header.jpg</c>.</summary>
public interface IMediaStore
{
    /// <summary>
    /// Stores <paramref name="files"/> under <paramref name="folder"/>, replacing files of the same name, and renders the
    /// <paramref name="prewarm"/> variants of each. Returns the stored path by file name; a file missing from it was
    /// rejected (bad content, too large).
    /// </summary>
    /// <exception cref="Exceptions.MediaStoreException">The store is unreachable, not configured or failed.</exception>
    Task<IReadOnlyDictionary<string, string>> UploadAsync(string folder, IReadOnlyList<MediaFile> files,
        IReadOnlyCollection<string> prewarm, CancellationToken cancellationToken = default);
}

public sealed record MediaFile(string Name, byte[] Content);
