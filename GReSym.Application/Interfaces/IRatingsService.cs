using GReSym.Application.DTO.Ratings;

namespace GReSym.Application.Interfaces;

public interface IRatingsService
{
    Task<GameRatingsListResponseDto> GetRatings(int userId, GameRatingsListRequestDto request);
    Task<GameRatingDto> RateGame(int userId, int gameId, RateGameRequestDto request);
    Task DeleteRating(int userId, int gameId);
}
