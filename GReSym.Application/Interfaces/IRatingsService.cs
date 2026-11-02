using GReSym.Application.DTO.Ratings;
using GReSym.Application.DTO.Users;

namespace GReSym.Application.Interfaces;

public interface IRatingsService
{
    Task<GameRatingsListResponseDto> GetRatings(int userId, GameRatingsListRequestDto request);
    Task<UserStatsDto> GetStats(int userId);
    Task<GameRatingDto> GetRating(int userId, int gameId);
    Task<GameRatingDto> RateGame(int userId, int gameId, RateGameRequestDto request);
    Task DeleteRating(int userId, int gameId);
}
