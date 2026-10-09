namespace GReSym.Core.Interfaces;

/// <summary>Asks the ML service to (re)build game vectors. Processing is asynchronous.</summary>
public interface IGameVectorizationQueue
{
    /// <exception cref="Exceptions.MessageQueueException">The broker is unavailable or rejected the message.</exception>
    Task EnqueueAsync(IReadOnlyCollection<int> gameIds, string reason, CancellationToken cancellationToken = default);
}
