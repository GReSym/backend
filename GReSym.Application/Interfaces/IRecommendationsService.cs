using GReSym.Application.DTO.Recommendations;

namespace GReSym.Application.Interfaces;

public interface IRecommendationsService
{
    /// <summary>Queues a recommendation for the ML worker, or fills in popular games right away (no usable ratings, broker down).</summary>
    Task<CreateRecommendationResponseDto> Create(int userId, RecommendationsRequestDto request,
        CancellationToken cancellationToken = default);

    /// <exception cref="Core.Exceptions.RecommendationNotFoundException">Unknown, or another user's.</exception>
    Task<RecommendationDto> Get(int userId, int id);

    /// <summary>Newest first, without games.</summary>
    Task<RecommendationHistoryDto> GetHistory(int userId, int page, int limit);

    /// <exception cref="Core.Exceptions.RecommendationNotFoundException">
    /// Unknown recommendation, another user's, or the game isn't in it (e.g. capped away).
    /// </exception>
    Task SetFeedback(int userId, int id, int gameId, bool? liked);
}
