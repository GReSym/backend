using GReSym.Application.DTO.Ratings;
using GReSym.Application.Interfaces;
using GReSym.Application.Mapping;
using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;

namespace GReSym.Application.Services;

public class RatingsService : IRatingsService
{
    private readonly IUserGameRateRepository _rateRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RatingsService(
        IUserGameRateRepository rateRepository,
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork)
    {
        _rateRepository = rateRepository;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GameRatingsListResponseDto> GetRatings(int userId, GameRatingsListRequestDto request)
    {
        var rates = await _rateRepository.GetByUserAsync(userId, request.Page, request.Limit);

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

    /// <summary>Creates or replaces the user's rating of the game.</summary>
    public async Task<GameRatingDto> RateGame(int userId, int gameId, RateGameRequestDto request)
    {
        if (request.Rating == null && request.Recommend == null)
            throw new DomainException("Either rating or recommend is required.");

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
