using GReSym.Core.Models;

namespace GReSym.Core.Interfaces;

/// <summary>Asks the ML worker to compute a recommendation. The result comes back asynchronously.</summary>
public interface IRecommendationQueue
{
    /// <exception cref="Exceptions.MessageQueueException">The broker is unavailable or rejected the message.</exception>
    Task EnqueueAsync(RecommendationTask task, CancellationToken cancellationToken = default);
}
