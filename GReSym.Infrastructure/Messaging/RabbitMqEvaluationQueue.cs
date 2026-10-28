using GReSym.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GReSym.Infrastructure.Messaging;

/// <summary>Publishes evaluation runs for the ML worker. Its reports come back through <see cref="EvaluationResultsConsumer"/>.</summary>
public sealed class RabbitMqEvaluationQueue : IEvaluationQueue
{
    private readonly RabbitMqPublisher _publisher;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEvaluationQueue> _logger;

    public RabbitMqEvaluationQueue(RabbitMqPublisher publisher, IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqEvaluationQueue> logger)
    {
        _publisher = publisher;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task EnqueueAsync(int runId, CancellationToken cancellationToken = default)
    {
        // Only the id: the worker reads the parameters from evaluation_runs
        await _publisher.PublishAsync(_settings.EvaluationRoutingKey, [new EvaluateMessage(runId)], cancellationToken);

        _logger.LogInformation("Queued evaluation run {RunId}", runId);
    }

    private record EvaluateMessage(int RunId);
}
