using GReSym.Core.Models;

namespace GReSym.Core.Interfaces;

/// <summary>Saves the ML worker's recommendation results (called by the results consumer, one scope per message).</summary>
public interface IRecommendationResultHandler
{
    Task HandleAsync(RecommendationResult result, CancellationToken cancellationToken = default);
}
