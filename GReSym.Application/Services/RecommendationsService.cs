using GReSym.Application.DTO.Recommendations;
using GReSym.Application.Interfaces;
using GReSym.Application.Mapping;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using GReSym.Core.Models;
using Microsoft.Extensions.Logging;

namespace GReSym.Application.Services;

/// <summary>
/// Personal recommendations from the ML service (content-based, from the user's ratings).
/// Falls back to popular games the user hasn't rated when there's nothing to go on or the ML service is down.
/// </summary>
public class RecommendationsService : IRecommendationsService
{
    private readonly IUserGameRateRepository _rateRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IRecommendationEngine _engine;
    private readonly ILogger<RecommendationsService> _logger;

    public RecommendationsService(
        IUserGameRateRepository rateRepository,
        IGameRepository gameRepository,
        IRecommendationEngine engine,
        ILogger<RecommendationsService> logger)
    {
        _rateRepository = rateRepository;
        _gameRepository = gameRepository;
        _engine = engine;
        _logger = logger;
    }

    public async Task<RecommendationsResponseDto> GetRecommendations(
        int userId,
        RecommendationsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var rates = (await _rateRepository.GetAllByUserAsync(userId)).ToList();
        var ratedIds = rates.Select(r => r.GameId).ToList();

        var signals = rates
            .Where(r => r.Rating != null || r.Recommend != null)
            .Select(r => new GameRatingSignal(r.GameId, r.Rating, r.Recommend))
            .ToList();

        if (signals.Count > 0)
        {
            var personal = await GetPersonal(userId, signals, request.Limit, cancellationToken);
            if (personal != null)
                return personal;
        }

        return await GetPopular(ratedIds, request.Limit);
    }

    private async Task<RecommendationsResponseDto?> GetPersonal(
        int userId,
        List<GameRatingSignal> signals,
        int limit,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ScoredGame> scored;
        try
        {
            scored = await _engine.RecommendAsync(signals, limit, cancellationToken);
        }
        catch (RecommendationEngineException e)
        {
            _logger.LogWarning(e, "Recommendation engine failed for user {UserId}, falling back to popular games", userId);
            return null;
        }

        // Empty when none of the rated games are vectorized yet, or the user only disliked games
        if (scored.Count == 0)
            return null;

        var games = (await _gameRepository.GetByIdsAsync(scored.Select(s => s.GameId).ToList()))
            .ToDictionary(g => g.Id);

        // Keep the engine's order. Skip games deleted from MariaDB but still present in Milvus.
        var result = scored
            .Where(s => games.ContainsKey(s.GameId))
            .Select(s => GameMapping.ToRecommendedDto(games[s.GameId], s.Score))
            .ToList();

        return new RecommendationsResponseDto
        {
            Source = RecommendationsResponseDto.SourcePersonal,
            Count = result.Count,
            Games = result
        };
    }

    private async Task<RecommendationsResponseDto> GetPopular(List<int> excludeIds, int limit)
    {
        var games = await _gameRepository.GetPopularGamesAsync(1, limit, excludeIds: excludeIds);

        var result = games
            .Select(g => GameMapping.ToRecommendedDto(g, null))
            .ToList();

        return new RecommendationsResponseDto
        {
            Source = RecommendationsResponseDto.SourcePopular,
            Count = result.Count,
            Games = result
        };
    }
}
