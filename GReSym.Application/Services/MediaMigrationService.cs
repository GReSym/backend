using GReSym.Application.DTO.Media;
using GReSym.Application.Interfaces;
using GReSym.Core.Common;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GReSym.Application.Services;

/// <summary>
/// Singleton. Each game: download its Steam-hosted images, upload them to <c>games/{id}/</c> in the media store
/// (prewarming the presets), then replace the URLs in the DB with the stored paths. Games are taken in batches, most
/// popular first, and saved per batch, so a stopped run loses at most the batch in progress (its files get replaced
/// on the next run). Games that failed are skipped for the rest of the run and keep their Steam URLs.
/// </summary>
public sealed class MediaMigrationService : IMediaMigrationService
{
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MediaSettings _settings;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<MediaMigrationService> _logger;
    private readonly Lock _lock = new();

    private Run? _run;
    private CancellationTokenSource? _cancellation;

    public MediaMigrationService(IServiceScopeFactory scopeFactory, IOptions<MediaSettings> settings,
        IHostApplicationLifetime lifetime, ILogger<MediaMigrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    public MediaMigrationStatusDto Start(StartMediaMigrationRequestDto request)
    {
        lock (_lock)
        {
            if (_run is { State: RunState.Running })
                throw new DomainException("A media migration is already running.");

            _cancellation?.Dispose();
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);
            _run = new Run(request.Limit, request.IncludeScreenshots, request.GameIds?.Distinct().ToList());
            var run = _run;
            var token = _cancellation.Token;
            _ = Task.Run(() => ExecuteAsync(run, token), CancellationToken.None);

            return run.ToDto();
        }
    }

    public MediaMigrationStatusDto GetStatus()
    {
        lock (_lock)
        {
            return _run?.ToDto() ?? new MediaMigrationStatusDto { State = "idle" };
        }
    }

    public bool Cancel()
    {
        lock (_lock)
        {
            if (_run is not { State: RunState.Running })
                return false;

            _cancellation?.Cancel();
            return true;
        }
    }

    public async Task<MediaStatsDto> GetStats(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var games = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var counts = await games.CountMediaAsync(cancellationToken);

        return new MediaStatsDto
        {
            Headers = counts.Headers,
            SteamHeaders = counts.ExternalHeaders,
            Screenshots = counts.Screenshots,
            SteamScreenshots = counts.ExternalScreenshots
        };
    }

    private async Task ExecuteAsync(Run run, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Media migration started: up to {Limit} games, screenshots: {Screenshots}",
            run.Limit, run.IncludeScreenshots);
        var failedIds = new HashSet<int>();
        var remaining = run.Limit;
        try
        {
            while (remaining > 0 && !cancellationToken.IsCancellationRequested)
            {
                var done = await MigrateBatchAsync(run, Math.Min(remaining, BatchSize), failedIds, cancellationToken);
                if (done == 0)
                    break;
                remaining -= done;
            }

            run.Finish(cancellationToken.IsCancellationRequested ? RunState.Cancelled : RunState.Completed, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            run.Finish(RunState.Cancelled, null);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Media migration failed");
            run.Finish(RunState.Failed, e is MediaStoreException ? e.Message : $"{e.GetType().Name}: {e.Message}");
        }

        _logger.LogInformation(
            "Media migration {State}: {Games} games, {Headers} headers, {Screenshots} screenshots, {Failed} files failed",
            run.State, run.GamesProcessed, run.HeadersMigrated, run.ScreenshotsMigrated, run.FilesFailed);
    }

    /// <returns>Games taken (0 = nothing left).</returns>
    private async Task<int> MigrateBatchAsync(Run run, int count, HashSet<int> failedIds,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var games = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        // Typed HTTP clients: taken per batch so their handlers rotate as IHttpClientFactory intends
        var transfer = new Transfer(scope.ServiceProvider.GetRequiredService<IExternalImageSource>(),
            scope.ServiceProvider.GetRequiredService<IMediaStore>());

        var ids = await games.GetIdsWithExternalImagesAsync(count, run.IncludeScreenshots, run.GameIds, failedIds,
            cancellationToken);
        if (ids.Count == 0)
            return 0;

        var batch = await games.GetWithScreenshotsByIdsAsync(ids, cancellationToken);
        try
        {
            // Only entity properties change in parallel, the DbContext is used again after all games are done
            await Parallel.ForEachAsync(batch,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, _settings.MigrationConcurrency),
                    CancellationToken = cancellationToken
                },
                async (game, token) =>
                {
                    if (!await MigrateGameAsync(run, transfer, game, token))
                    {
                        lock (failedIds)
                        {
                            failedIds.Add(game.Id);
                        }
                    }
                });
        }
        finally
        {
            // Also when cancelled or failed halfway: the games already uploaded keep their new paths
            await unitOfWork.SaveChangesAsync();
        }

        return ids.Count;
    }

    /// <returns>False if some image couldn't be moved.</returns>
    private async Task<bool> MigrateGameAsync(Run run, Transfer transfer, Game game,
        CancellationToken cancellationToken)
    {
        var folder = MediaPaths.GameFolder(game.Id);
        var ok = true;

        if (MediaPaths.IsExternal(game.HeaderImageUrl))
        {
            var url = game.HeaderImageUrl!;
            var name = MediaPaths.HeaderFileName(url);
            var stored = await TransferAsync(run, transfer, folder, [(name, url)], _settings.HeaderPresets, cancellationToken);
            if (stored.TryGetValue(name, out var path))
            {
                game.HeaderImageUrl = path;
                Interlocked.Increment(ref run.HeadersMigrated);
            }
            else
            {
                ok = false;
            }
        }

        if (run.IncludeScreenshots)
        {
            var screenshots = game.Screenshots
                .Where(s => MediaPaths.IsExternal(s.Url))
                .Select(s => (Screenshot: s, Name: MediaPaths.ScreenshotFileName(s.Id, s.Url)))
                .ToList();
            if (screenshots.Count > 0)
            {
                var stored = await TransferAsync(run, transfer, folder, screenshots.Select(s => (s.Name, s.Screenshot.Url)).ToList(),
                    _settings.ScreenshotPresets, cancellationToken);
                foreach (var (screenshot, name) in screenshots)
                {
                    if (stored.TryGetValue(name, out var path))
                    {
                        screenshot.Url = path;
                        Interlocked.Increment(ref run.ScreenshotsMigrated);
                    }
                    else
                    {
                        ok = false;
                    }
                }
            }
        }

        Interlocked.Increment(ref run.GamesProcessed);
        if (!ok)
            Interlocked.Increment(ref run.GamesFailed);
        return ok;
    }

    /// <summary>Downloads and uploads <paramref name="files"/>; returns the stored path by name of those that made it.</summary>
    private async Task<IReadOnlyDictionary<string, string>> TransferAsync(Run run, Transfer transfer, string folder,
        IReadOnlyList<(string Name, string Url)> files, IReadOnlyCollection<string> presets,
        CancellationToken cancellationToken)
    {
        var downloaded = new List<MediaFile>();
        foreach (var (name, url) in files)
        {
            byte[]? content;
            try
            {
                content = await transfer.Source.DownloadAsync(url, cancellationToken);
            }
            catch (Exception e) when (e is HttpRequestException
                                          or TaskCanceledException { InnerException: TimeoutException })
            {
                _logger.LogWarning("Downloading {Url} failed: {Error}", url, e.Message);
                content = null;
            }

            if (content == null)
            {
                _logger.LogInformation("Skipping {Folder}/{Name}: {Url} is not available", folder, name, url);
                continue;
            }
            downloaded.Add(new MediaFile(name, content));
        }

        var stored = await transfer.Store.UploadAsync(folder, downloaded, presets, cancellationToken);

        Interlocked.Add(ref run.FilesFailed, files.Count - stored.Count);
        return stored;
    }

    private sealed record Transfer(IExternalImageSource Source, IMediaStore Store);

    private enum RunState
    {
        Running,
        Completed,
        Cancelled,
        Failed
    }

    /// <summary>Counters are updated with Interlocked from the parallel games.</summary>
    private sealed class Run(int limit, bool includeScreenshots, IReadOnlyCollection<int>? gameIds)
    {
        public int GamesProcessed;
        public int GamesFailed;
        public int HeadersMigrated;
        public int ScreenshotsMigrated;
        public int FilesFailed;

        public int Limit { get; } = limit;
        public bool IncludeScreenshots { get; } = includeScreenshots;
        public IReadOnlyCollection<int>? GameIds { get; } = gameIds;
        public DateTime StartedAt { get; } = DateTime.UtcNow;
        public RunState State { get; private set; } = RunState.Running;
        public DateTime? FinishedAt { get; private set; }
        public string? Error { get; private set; }

        public void Finish(RunState state, string? error)
        {
            Error = error;
            FinishedAt = DateTime.UtcNow;
            State = state;
        }

        public MediaMigrationStatusDto ToDto() => new()
        {
            State = State.ToString().ToLowerInvariant(),
            StartedAt = StartedAt,
            FinishedAt = FinishedAt,
            Limit = Limit,
            IncludeScreenshots = IncludeScreenshots,
            GamesProcessed = Volatile.Read(ref GamesProcessed),
            GamesFailed = Volatile.Read(ref GamesFailed),
            HeadersMigrated = Volatile.Read(ref HeadersMigrated),
            ScreenshotsMigrated = Volatile.Read(ref ScreenshotsMigrated),
            FilesFailed = Volatile.Read(ref FilesFailed),
            Error = Error
        };
    }
}
