using RabbitMQ.Client;

namespace GReSym.Infrastructure.Messaging;

/// <summary>Every exchange and queue of GReSym. Idempotent. Must stay identical to declare_topology in ml/src/ml/worker.py.</summary>
public static class RabbitMqTopology
{
    public static async Task DeclareAsync(IChannel channel, RabbitMqSettings settings, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(settings.Exchange, ExchangeType.Direct, durable: true,
            cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(settings.DeadLetterExchange, ExchangeType.Direct, durable: true,
            cancellationToken: cancellationToken);

        await DeclareWorkQueueAsync(channel, settings, settings.VectorizeQueue, settings.VectorizeDeadLetterQueue,
            settings.VectorizeRoutingKey, settings.DeliveryLimit, cancellationToken);
        await DeclareWorkQueueAsync(channel, settings, settings.EvaluationQueue, settings.EvaluationDeadLetterQueue,
            settings.EvaluationRoutingKey, settings.EvaluationDeliveryLimit, cancellationToken);
        await DeclareWorkQueueAsync(channel, settings, settings.EvaluationResultsQueue,
            settings.EvaluationResultsDeadLetterQueue, settings.EvaluationResultsRoutingKey, settings.DeliveryLimit,
            cancellationToken);
        await DeclareWorkQueueAsync(channel, settings, settings.RecommendationQueue,
            settings.RecommendationDeadLetterQueue, settings.RecommendationRoutingKey,
            settings.RecommendationDeliveryLimit, cancellationToken);
        await DeclareWorkQueueAsync(channel, settings, settings.RecommendationResultsQueue,
            settings.RecommendationResultsDeadLetterQueue, settings.RecommendationResultsRoutingKey,
            settings.DeliveryLimit, cancellationToken);
    }

    /// <summary>Quorum queue bound to the exchange, dead-lettered to its .dlq with the same routing key.</summary>
    private static async Task DeclareWorkQueueAsync(IChannel channel, RabbitMqSettings settings, string queue,
        string deadLetterQueue, string routingKey, int deliveryLimit, CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-delivery-limit"] = deliveryLimit,
                ["x-dead-letter-exchange"] = settings.DeadLetterExchange,
            },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queue, settings.Exchange, routingKey, cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(deadLetterQueue, settings.DeadLetterExchange, routingKey,
            cancellationToken: cancellationToken);
    }
}
