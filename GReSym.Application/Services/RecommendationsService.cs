using GReSym.Application.DTO.Recommendations;
using GReSym.Application.Interfaces;
using GReSym.Application.Mapping;
using GReSym.Core.Common;
using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Enums;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using GReSym.Core.Models;
using Microsoft.Extensions.Logging;

namespace GReSym.Application.Services;

/// <summary>
/// Personal recommendations, computed asynchronously by the ML worker (content-based, from the user's ratings) and kept
/// as the user's history. Popular games the user hasn't rated are the fallback when there's nothing to go on.
/// </summary>
public class RecommendationsService : IRecommendationsService
{
    private readonly IUserRecommendationRepository _recommendations;
    private readonly IUserGameRateRepository _rateRepository;
    private readonly IUserGameListRepository _lists;
    private readonly IGameRepository _gameRepository;
    private readonly IUserSettingsService _settingsService;
    private readonly IRecommendationQueue _queue;
    private readonly RecommendationResultHandler _resultHandler;
    private readonly ILogger<RecommendationsService> _logger;

    public RecommendationsService(
        IUserRecommendationRepository recommendations,
        IUserGameRateRepository rateRepository,
        IUserGameListRepository lists,
        IGameRepository gameRepository,
        IUserSettingsService settingsService,
        IRecommendationQueue queue,
        RecommendationResultHandler resultHandler,
        ILogger<RecommendationsService> logger)
    {
        _recommendations = recommendations;
        _rateRepository = rateRepository;
        _lists = lists;
        _gameRepository = gameRepository;
        _settingsService = settingsService;
        _queue = queue;
        _resultHandler = resultHandler;
        _logger = logger;
    }

    public async Task<CreateRecommendationResponseDto> Create(
        int userId,
        RecommendationsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var signals = (await _rateRepository.GetAllByUserAsync(userId))
            .Where(r => r.Rating != null || r.Recommend != null)
            .Select(r => new GameRatingSignal(r.GameId, r.Rating, r.Recommend))
            .ToList();
        // A rating is a stronger signal than a list entry (a rated game normally leaves its list anyway)
        var ratedIds = signals.Select(s => s.GameId).ToHashSet();
        var listed = (await _lists.GetAllByUserAsync(userId)).Where(e => !ratedIds.Contains(e.GameId)).ToList();
        var planned = listed.Where(e => e.Status == GameListStatus.Planned).Select(e => e.GameId).ToList();
        var rejected = listed.Where(e => e.Status == GameListStatus.Rejected).Select(e => e.GameId).ToList();
        var tuning = ApplyOverrides(await _settingsService.GetTuning(userId), request);

        var recommendation = new UserRecommendation
        {
            UserId = userId,
            Status = RecommendationStatus.Queued,
            GameLimit = request.Limit,
            Mainstream = tuning.Mainstream,
            DislikeMode = tuning.DislikeMode,
            DislikeStrength = tuning.DislikeStrength,
        };
        await _recommendations.AddAsync(recommendation);
        await _recommendations.SaveChangesAsync(cancellationToken);

        // Rejected games alone give nothing to go on: popular games (without them) right away
        if (signals.Count == 0 && planned.Count == 0)
        {
            await _resultHandler.CompletePopularAsync(recommendation, null, CancellationToken.None);
        }
        else
        {
            try
            {
                await _queue.EnqueueAsync(
                    new RecommendationTask(recommendation.Id, signals, planned, rejected, request.Limit, tuning), cancellationToken);
            }
            catch (MessageQueueException e)
            {
                _logger.LogWarning(e, "Recommendation {RecommendationId} could not be queued, falling back to popular games",
                    recommendation.Id);
                await _resultHandler.CompletePopularAsync(recommendation, "Message broker is unavailable.",
                    CancellationToken.None);
            }
        }

        return new CreateRecommendationResponseDto { Id = recommendation.Id, Status = recommendation.Status };
    }

    public async Task<RecommendationDto> Get(int userId, int id)
    {
        var recommendation = await _recommendations.GetWithGamesAsync(id);
        if (recommendation is null || recommendation.UserId != userId)
            throw new RecommendationNotFoundException(id);

        var games = (await _gameRepository.GetByIdsAsync(recommendation.Games.Select(g => g.GameId).ToList()))
            .ToDictionary(g => g.Id);

        var result = ToDto(recommendation, recommendation.Games.Count);
        result.Games = recommendation.Games
            .Where(g => games.ContainsKey(g.GameId))
            .Select(g => GameMapping.ToRecommendedDto(games[g.GameId], g))
            .ToList();
        result.Count = result.Games.Count;
        return result;
    }

    public async Task<RecommendationHistoryDto> GetHistory(int userId, int page, int limit)
    {
        if (page < 1)
            throw new DomainException("Page must be at least 1.");
        if (limit is < 1 or > Constants.Api.MaxPageSize)
            throw new DomainException($"Limit must be between 1 and {Constants.Api.MaxPageSize}.");

        var rows = await _recommendations.GetHistoryAsync(userId, page, limit);

        return new RecommendationHistoryDto
        {
            Count = rows.Count,
            Recommendations = rows.Select(r => ToDto(r.Recommendation, r.GameCount)).ToList(),
        };
    }

    public async Task SetFeedback(int userId, int id, int gameId, bool? liked)
    {
        var recommendation = await _recommendations.GetAsync(id);
        if (recommendation is null || recommendation.UserId != userId)
            throw new RecommendationNotFoundException(id);

        var game = await _recommendations.GetGameAsync(id, gameId)
                   ?? throw new RecommendationNotFoundException(id, gameId);

        var previous = game.Liked;
        game.Liked = liked;
        recommendation.UpdatedAt = DateTime.UtcNow;
        await SyncGameListAsync(userId, gameId, previous, liked);
        await _recommendations.SaveChangesAsync();
    }

    /// <summary>
    /// Feedback fills the lists: liked → planned, disliked → rejected. Clearing it removes the game from the list only if it's
    /// still in the one the old feedback put it in (it may have been moved on the game page since). Games the user rated in
    /// the library aren't listed. Saved together with the feedback.
    /// </summary>
    private async Task SyncGameListAsync(int userId, int gameId, bool? previous, bool? liked)
    {
        if (liked is { } value)
        {
            var rate = await _rateRepository.GetAsync(userId, gameId);
            if (rate is { Rating: null, Recommend: null } or null)
                await GameListsService.SetAsync(_lists, userId, gameId, GameListStatuses.FromFeedback(value));
            return;
        }

        if (previous is not { } old)
            return;
        var entry = await _lists.GetAsync(userId, gameId);
        if (entry != null && entry.Status == GameListStatuses.FromFeedback(old))
            _lists.Remove(entry);
    }

    /// <summary>The request's tuning fields win over the stored settings (one-off preview).</summary>
    private static RecommendationTuning ApplyOverrides(RecommendationTuning stored, RecommendationsRequestDto request) => new(
        request.Mainstream ?? stored.Mainstream,
        request.DislikeMode != null ? DislikeModes.Parse(request.DislikeMode) : stored.DislikeMode,
        request.DislikeStrength ?? stored.DislikeStrength);

    private static RecommendationDto ToDto(UserRecommendation recommendation, int count) => new()
    {
        Id = recommendation.Id,
        Status = recommendation.Status,
        Source = recommendation.Source,
        Limit = recommendation.GameLimit,
        Mainstream = recommendation.Mainstream,
        DislikeMode = recommendation.DislikeMode.ToName(),
        DislikeStrength = recommendation.DislikeStrength,
        UsedRatings = recommendation.UsedRatings,
        Error = recommendation.Error,
        CreatedAt = recommendation.CreatedAt,
        UpdatedAt = recommendation.UpdatedAt,
        FinishedAt = recommendation.FinishedAt,
        Count = count,
    };
}
