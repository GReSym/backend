using System.Text.Json;
using GReSym.Application.DTO.Evaluation;
using GReSym.Application.Interfaces;
using GReSym.Core.Common;
using GReSym.Core.Entities.Evaluation;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GReSym.Application.Services;

/// <summary>Offline evaluation: importing collected Steam users and running evaluations on the ML worker.</summary>
public class EvaluationService : IEvaluationService
{
    private const int SupportedImportVersion = 1;
    private static readonly string[] TestSelections = ["top", "random-liked"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEvaluationRepository _evaluation;
    private readonly IEvaluationQueue _queue;
    private readonly ILogger<EvaluationService> _logger;

    public EvaluationService(IEvaluationRepository evaluation, IEvaluationQueue queue, ILogger<EvaluationService> logger)
    {
        _evaluation = evaluation;
        _queue = queue;
        _logger = logger;
    }

    public async Task<ImportSteamUsersResponseDto> ImportSteamUsers(ImportSteamUsersRequestDto request)
    {
        if (request.Version != SupportedImportVersion)
            throw new DomainException($"Unsupported file version {request.Version}, expected {SupportedImportVersion}.");
        if (request.Users.DistinctBy(u => u.SteamIdHash).Count() != request.Users.Count
            || request.Users.DistinctBy(u => u.Name).Count() != request.Users.Count)
            throw new DomainException("The file has duplicate user names or hashes.");

        var existing = await _evaluation.GetSteamUserKeysAsync();
        var existingHashes = existing.Select(u => u.SteamIdHash).ToHashSet();
        var namesByHash = existing.ToDictionary(u => u.Name, u => u.SteamIdHash);

        var newUsers = request.Users.Where(u => !existingHashes.Contains(u.SteamIdHash)).ToList();
        var clash = newUsers.FirstOrDefault(u => namesByHash.ContainsKey(u.Name));
        if (clash != null)
            throw new DomainException($"User name {clash.Name} is already taken by another Steam user.");

        var gameIds = await _evaluation.GetGameIdsBySteamAppIdsAsync(
            newUsers.SelectMany(u => u.Games).Select(g => g.SteamAppId).ToList());

        var now = DateTime.UtcNow;
        var result = new ImportSteamUsersResponseDto { SkippedExisting = request.Users.Count - newUsers.Count };
        var entities = new List<SteamUser>();

        foreach (var user in newUsers)
        {
            var games = user.Games
                .Where(g => gameIds.ContainsKey(g.SteamAppId))
                .DistinctBy(g => gameIds[g.SteamAppId])
                .Select(g => new SteamUserGame
                {
                    GameId = gameIds[g.SteamAppId],
                    PlaytimeMinutes = g.PlaytimeMinutes,
                    Recommended = g.Recommended,
                })
                .ToList();

            result.GamesMatched += games.Count;
            result.GamesUnmatched += user.Games.Count - games.Count;
            entities.Add(new SteamUser
            {
                Name = user.Name,
                SteamIdHash = user.SteamIdHash,
                GamesOwned = user.GamesOwned,
                GamesPlayed = user.GamesPlayed,
                ReviewsCount = user.ReviewsCount,
                CollectedAt = request.CollectedAt?.ToUniversalTime(),
                CreatedAt = now,
                Games = games,
            });
        }

        // One SaveChanges: all users or none
        await _evaluation.AddSteamUsersAsync(entities);
        await _evaluation.SaveChangesAsync();

        result.Imported = entities.Count;
        _logger.LogInformation("Imported {Imported} Steam users ({Skipped} already present), {Matched} games matched, {Unmatched} not in the catalogue",
            result.Imported, result.SkippedExisting, result.GamesMatched, result.GamesUnmatched);
        return result;
    }

    public async Task<SteamUsersSummaryDto> GetSteamUsersSummary()
    {
        var summary = await _evaluation.GetSteamUsersSummaryAsync();

        return new SteamUsersSummaryDto
        {
            Users = summary.Users,
            Games = summary.Games,
            ReviewedGames = summary.ReviewedGames,
            AverageGamesPerUser = summary.AverageGamesPerUser,
        };
    }

    public async Task<StartEvaluationResponseDto> StartRun(StartEvaluationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var ks = request.Ks is { Count: > 0 } ? request.Ks.Distinct().Order().ToList() : [5, 10, 20];
        if (ks.Any(k => k is < 1 or > 100))
            throw new DomainException("Every K must be between 1 and 100.");

        var testSelection = request.TestSelection ?? "top";
        if (!TestSelections.Contains(testSelection))
            throw new DomainException($"Unknown testSelection {testSelection}. Known: {string.Join(", ", TestSelections)}.");

        var hybridBetas = request.HybridBetas is { Count: > 0 }
            ? request.HybridBetas.Distinct().Order().ToList()
            : [0.05, 0.1, 0.2, 0.4];
        if (hybridBetas.Any(b => b is <= 0 or > 10))
            throw new DomainException("Every hybrid β must be in (0, 10].");

        // Complete parameters are stored, so the run stays reproducible if the defaults change.
        // Names and defaults match EvaluationParams in ml/src/ml/evaluation.py
        var parameters = new
        {
            Ks = ks,
            TestSize = request.TestSize ?? 3,
            MinTrainGames = request.MinTrainGames ?? 5,
            MinPlaytimeMinutes = request.MinPlaytimeMinutes ?? 60,
            TestSelection = testSelection,
            Seed = request.Seed ?? 42,
            HybridBetas = hybridBetas,
            Center = request.Center ?? false,
        };

        var run = new EvaluationRun
        {
            Status = EvaluationStatus.Queued,
            Parameters = JsonSerializer.Serialize(parameters, JsonOptions),
            CreatedAt = DateTime.UtcNow,
        };
        await _evaluation.AddRunAsync(run);
        await _evaluation.SaveChangesAsync(cancellationToken);

        try
        {
            await _queue.EnqueueAsync(run.Id, cancellationToken);
        }
        catch (MessageQueueException e)
        {
            run.Status = EvaluationStatus.Failed;
            run.Error = "Message broker is unavailable.";
            run.FinishedAt = DateTime.UtcNow;
            await _evaluation.SaveChangesAsync(CancellationToken.None);
            _logger.LogWarning(e, "Evaluation run {RunId} could not be queued", run.Id);
            throw;
        }

        return new StartEvaluationResponseDto { RunId = run.Id };
    }

    public async Task<EvaluationRunsListDto> GetRuns(int page, int limit)
    {
        if (page < 1)
            throw new DomainException("Page must be at least 1.");
        if (limit is < 1 or > Constants.Api.MaxPageSize)
            throw new DomainException($"Limit must be between 1 and {Constants.Api.MaxPageSize}.");

        var runs = await _evaluation.GetRunsAsync(page, limit);

        return new EvaluationRunsListDto
        {
            Count = runs.Count,
            Runs = runs.Select(r => ToDto(r, withResults: false)).ToList(),
        };
    }

    public async Task<EvaluationRunDto> GetRun(int id)
    {
        var run = await _evaluation.GetRunAsync(id) ?? throw new EvaluationRunNotFoundException(id);

        return ToDto(run, withResults: true);
    }

    private static EvaluationRunDto ToDto(EvaluationRun run, bool withResults) => new()
    {
        Id = run.Id,
        Status = run.Status,
        Parameters = ParseJson(run.Parameters) ?? default,
        UsersEvaluated = run.UsersEvaluated,
        Error = run.Error,
        CreatedAt = run.CreatedAt,
        StartedAt = run.StartedAt,
        FinishedAt = run.FinishedAt,
        Results = withResults ? ParseJson(run.Results) : null,
    };

    private static JsonElement? ParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
