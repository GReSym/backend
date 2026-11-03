using GReSym.Application.DTO.Ratings;
using GReSym.Application.DTO.Users;
using GReSym.Application.Interfaces;
using GReSym.Application.Mapping;
using GReSym.Core.Common;
using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;

namespace GReSym.Application.Services;

public class RatingsService : IRatingsService
{
    private readonly IUserGameRateRepository _rateRepository;
    private readonly IUserGameListRepository _lists;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RatingsService(
        IUserGameRateRepository rateRepository,
        IUserGameListRepository lists,
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork)
    {
        _rateRepository = rateRepository;
        _lists = lists;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GameRatingsListResponseDto> GetRatings(int userId, GameRatingsListRequestDto request)
    {
        var rates = await _rateRepository.GetByUserAsync(userId, request.Page, request.Limit,
            request.MinHours, request.Unevaluated);

        var result = rates
            .Select(r =>
            {
                var dto = ToDto(r);
                dto.Game = GameMapping.ToDto(r.Game);
                return dto;
            })
            .ToList();

        return new GameRatingsListResponseDto
        {
            Count = result.Count,
            Ratings = result
        };
    }

    public async Task<UserStatsDto> GetStats(int userId)
    {
        var rates = (await _rateRepository.GetAllByUserWithTagsAsync(userId)).ToList();

        // Liked games count double, disliked ones are skipped: the favorite tag should describe what the user enjoys
        var tagWeights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var rate in rates)
        {
            bool liked = rate.Recommend == true || rate.Rating >= 7;
            bool disliked = rate.Recommend == false || rate.Rating <= 4;
            if (disliked && !liked)
                continue;

            int weight = liked ? 2 : 1;
            foreach (var tag in rate.Game.Tags)
            {
                if (Constants.Tags.StoreFeatures.Contains(tag.Name))
                    continue;
                tagWeights[tag.Name] = tagWeights.GetValueOrDefault(tag.Name) + weight;
            }
        }

        return new UserStatsDto
        {
            TotalHours = rates.Sum(r => (long)(r.PlayerHours ?? 0)),
            GamesPlayed = rates.Count,
            FavoriteTag = tagWeights.Count == 0
                ? null
                : tagWeights.MaxBy(kv => kv.Value).Key
        };
    }

    public async Task<GameRatingDto> GetRating(int userId, int gameId)
    {
        var rate = await _rateRepository.GetAsync(userId, gameId);

        if (rate == null)
            throw new RatingNotFoundException(gameId);

        return ToDto(rate);
    }

    /// <summary>
    /// Creates or replaces the user's rating of the game (an entry without rating and recommend is a library entry).
    /// A rating or recommend takes the game out of the planned / rejected lists.
    /// </summary>
    public async Task<GameRatingDto> RateGame(int userId, int gameId, RateGameRequestDto request)
    {
        if (!await _gameRepository.ExistsAsync(gameId))
            throw new GameNotFoundException(gameId.ToString());

        var rate = await _rateRepository.GetAsync(userId, gameId);

        if (rate == null)
        {
            rate = new UserGameRate { UserId = userId, GameId = gameId };
            await _rateRepository.AddAsync(rate);
        }

        rate.Rating = request.Rating;
        rate.Recommend = request.Recommend;
        rate.PlayerHours = request.PlayerHours;
        rate.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        if (rate.Rating != null || rate.Recommend != null)
        {
            var listed = await _lists.GetAsync(userId, gameId);
            if (listed != null)
                _lists.Remove(listed);
        }

        await _unitOfWork.SaveChangesAsync();

        return ToDto(rate);
    }

    public async Task DeleteRating(int userId, int gameId)
    {
        var rate = await _rateRepository.GetAsync(userId, gameId);

        if (rate == null)
            throw new RatingNotFoundException(gameId);

        _rateRepository.Remove(rate);
        await _unitOfWork.SaveChangesAsync();
    }

    private static GameRatingDto ToDto(UserGameRate rate)
    {
        return new GameRatingDto
        {
            GameId = rate.GameId,
            Rating = rate.Rating,
            Recommend = rate.Recommend,
            PlayerHours = rate.PlayerHours,
            Comment = rate.Comment
        };
    }
}
