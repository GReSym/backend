namespace GReSym.Core.Interfaces;

/// <summary>Asks the ML worker to run an offline evaluation. Results come back asynchronously.</summary>
public interface IEvaluationQueue
{
    /// <exception cref="Exceptions.MessageQueueException">The broker is unavailable or rejected the message.</exception>
    Task EnqueueAsync(int runId, CancellationToken cancellationToken = default);
}
