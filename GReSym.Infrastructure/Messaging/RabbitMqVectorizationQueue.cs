using GReSym.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GReSym.Infrastructure.Messaging;

/// <summary>Publishes game vectorization requests, consumed by the ML worker (ml/src/ml/worker.py).</summary>
public sealed class RabbitMqVectorizationQueue : IGameVectorizationQueue
{
    private readonly RabbitMqPublisher _publisher;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqVectorizationQueue> _logger;

    public RabbitMqVectorizationQueue(RabbitMqPublisher publisher, IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqVectorizationQueue> logger)
    {
        _publisher = publisher;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task EnqueueAsync(IReadOnlyCollection<int> gameIds, string reason, CancellationToken cancellationToken = default)
    {
        if (gameIds.Count == 0)
            return;

        await _publisher.PublishAsync(_settings.VectorizeRoutingKey,
            gameIds.Select(id => new VectorizeGameMessage(id, reason)), cancellationToken);

        _logger.LogInformation("Queued vectorization of {Count} game(s), reason {Reason}", gameIds.Count, reason);
    }

    private record VectorizeGameMessage(int GameId, string Reason);
}
