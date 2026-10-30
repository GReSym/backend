namespace GReSym.Core.Exceptions;

public class RecommendationNotFoundException : DomainException
{
    public RecommendationNotFoundException(int recommendationId)
        : base($"Recommendation {recommendationId} was not found.") { }

    public RecommendationNotFoundException(int recommendationId, int gameId)
        : base($"Game {gameId} is not in recommendation {recommendationId}.") { }
}
