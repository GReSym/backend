using GReSym.Core.Enums;
using GReSym.Core.Interfaces;
using GReSym.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GReSym.Infrastructure.Messaging;

/// <summary>Publishes recommendation tasks for the ML worker. Results come back through <see cref="RecommendationResultsConsumer"/>.</summary>
public sealed class RabbitMqRecommendationQueue : IRecommendationQueue
{
    private readonly RabbitMqPublisher _publisher;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqRecommendationQueue> _logger;

    public RabbitMqRecommendationQueue(RabbitMqPublisher publisher, IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqRecommendationQueue> logger)
    {
        _publisher = publisher;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task EnqueueAsync(RecommendationTask task, CancellationToken cancellationToken = default)
    {
        // The whole input travels in the message: the ML service doesn't read users_games_rates.
        // Same fields as RecommendationsRequest in ml/src/ml/personal.py
        var message = new RecommendMessage(
            task.RecommendationId,
            task.Ratings.Select(r => new RatingItem(r.GameId, r.Rating, r.Recommend)).ToList(),
            task.Planned.ToList(),
            task.Rejected.ToList(),
            task.Limit,
            task.Tuning.Mainstream,
            task.Tuning.DislikeMode.ToName(),
            task.Tuning.DislikeStrength);

        await _publisher.PublishAsync(_settings.RecommendationRoutingKey, [message], cancellationToken);

        _logger.LogInformation(
            "Queued recommendation {RecommendationId} ({Ratings} ratings, {Planned} planned, {Rejected} rejected)",
            task.RecommendationId, task.Ratings.Count, task.Planned.Count, task.Rejected.Count);
    }

    private record RatingItem(int GameId, int? Rating, bool? Recommend);
    private record RecommendMessage(
        int RecommendationId, List<RatingItem> Ratings, List<int> Planned, List<int> Rejected, int Limit, int Mainstream,
        string DislikeMode, int DislikeStrength);
}
