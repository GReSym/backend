using GReSym.Application.DTO.Recommendations;

namespace GReSym.Application.Interfaces;

public interface IRecommendationsService
{
    Task<RecommendationsResponseDto> GetRecommendations(int userId, RecommendationsRequestDto request, CancellationToken cancellationToken = default);
}
