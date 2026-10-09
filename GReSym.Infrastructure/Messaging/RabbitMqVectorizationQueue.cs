using System.Text.Json;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace GReSym.Infrastructure.Messaging;

/// <summary>
/// Publishes game vectorization requests, consumed by the ML worker (ml/src/ml/worker.py).
/// Singleton: one connection and one confirm-enabled channel per process, opened lazily on first use,
/// so the API starts even when the broker is down.
/// </summary>
public sealed class RabbitMqVectorizationQueue : IGameVectorizationQueue, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqVectorizationQueue> _logger;
    // Channels aren't safe for concurrent publishing, so publishes are serialized
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqVectorizationQueue(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqVectorizationQueue> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task EnqueueAsync(IReadOnlyCollection<int> gameIds, string reason, CancellationToken cancellationToken = default)
    {
        if (gameIds.Count == 0)
            return;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);

            foreach (var gameId in gameIds)
            {
                var body = JsonSerializer.SerializeToUtf8Bytes(new VectorizeGameMessage(gameId, reason), JsonOptions);
                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    Type = _settings.VectorizeRoutingKey,
                };

                // With confirmation tracking this completes once the broker has the message;
                // mandatory: true makes an unroutable message fail instead of vanishing
                await channel.BasicPublishAsync(
                    _settings.Exchange, _settings.VectorizeRoutingKey, mandatory: true,
                    basicProperties: properties, body: body, cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Queued vectorization of {Count} game(s), reason {Reason}", gameIds.Count, reason);
        }
        catch (Exception e) when (e is BrokerUnreachableException or OperationInterruptedException
                                      or PublishException or AlreadyClosedException)
        {
            throw new MessageQueueException("Failed to publish vectorization request", e);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        if (_connection is not { IsOpen: true })
        {
            if (_connection != null)
                await _connection.DisposeAsync();

            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                VirtualHost = _settings.VirtualHost,
                UserName = _settings.User,
                Password = _settings.Password,
                ClientProvidedName = "gresym-api",
                AutomaticRecoveryEnabled = true,
            };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
        }

        if (_channel != null)
            await _channel.DisposeAsync();

        var options = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        _channel = await _connection.CreateChannelAsync(options, cancellationToken);

        await DeclareTopologyAsync(_channel, cancellationToken);
        return _channel;
    }

    /// <summary>Idempotent. Must stay identical to declare_topology in ml/src/ml/worker.py.</summary>
    private async Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(_settings.Exchange, ExchangeType.Direct, durable: true,
            cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(_settings.DeadLetterExchange, ExchangeType.Direct, durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(_settings.VectorizeQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-delivery-limit"] = _settings.DeliveryLimit,
                ["x-dead-letter-exchange"] = _settings.DeadLetterExchange,
            },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(_settings.VectorizeQueue, _settings.Exchange, _settings.VectorizeRoutingKey,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(_settings.VectorizeDeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(_settings.VectorizeDeadLetterQueue, _settings.DeadLetterExchange,
            _settings.VectorizeRoutingKey, cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
            await _channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
        _lock.Dispose();
    }

    private record VectorizeGameMessage(int GameId, string Reason);
}
