using GReSym.Application.DTO.Reviews;

namespace GReSym.Application.Interfaces;

public interface IReviewsService
{
    Task<GameReviewsListDto> GetGameReviews(int gameId, GameReviewsListRequestDto request);
    Task<GameReviewsListDto> GetUserReviews(int userId, GameReviewsListRequestDto request);
    Task<GameReviewDto> UpdateUserReview(int gameId, int userId, AddReviewRequestDto request);
}