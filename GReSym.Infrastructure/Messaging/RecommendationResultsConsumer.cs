using System.Text.Json;
using GReSym.Core.Interfaces;
using GReSym.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GReSym.Infrastructure.Messaging;

/// <summary>
/// Passes the ML worker's recommendation results ({recommendationId, status: completed|failed, items?, usedRatings?, error?, at})
/// to <see cref="IRecommendationResultHandler"/>.
/// </summary>
public sealed class RecommendationResultsConsumer : RabbitMqResultsConsumer
{
    private const string StatusCompleted = "completed";
    private const string StatusFailed = "failed";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecommendationResultsConsumer> _logger;

    public RecommendationResultsConsumer(IOptions<RabbitMqSettings> settings, IServiceScopeFactory scopeFactory,
        ILogger<RecommendationResultsConsumer> logger)
        : base(settings.Value, logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override string QueueName => Settings.RecommendationResultsQueue;

    protected override async Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        ResultMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<ResultMessage>(delivery.Body.Span, JsonOptions);
        }
        catch (JsonException)
        {
            message = null;
        }

        if (message is null || message.RecommendationId <= 0
            || message.Status is not (StatusCompleted or StatusFailed))
        {
            _logger.LogError("Malformed recommendation result {MessageId}, dead-lettering",
                delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }

        var result = new RecommendationResult(
            message.RecommendationId,
            message.Status == StatusCompleted,
            message.Items?.Select(i => new ScoredGame(i.GameId, i.Score)).ToList() ?? [],
            message.UsedRatings,
            message.Error,
            message.At?.UtcDateTime ?? DateTime.UtcNow);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRecommendationResultHandler>();
            await handler.HandleAsync(result, cancellationToken);

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception e) when (e is DbUpdateException or InvalidOperationException
                                      or MySqlConnector.MySqlException)
        {
            // Requeued; after x-delivery-limit redeliveries the broker dead-letters it
            _logger.LogError(e, "Saving recommendation {RecommendationId} failed", message.RecommendationId);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }

    private record ScoredItem(int GameId, double Score);
    private record ResultMessage(
        int RecommendationId,
        string Status,
        List<ScoredItem>? Items,
        int? UsedRatings,
        string? Error,
        DateTimeOffset? At);
}
