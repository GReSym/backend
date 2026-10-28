using System.Text.Json;
using GReSym.Core.Exceptions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace GReSym.Infrastructure.Messaging;

/// <summary>
/// Publishes JSON messages to the tasks exchange (consumed by the ML worker, ml/src/ml/worker.py).
/// Singleton: one connection and one confirm-enabled channel per process, opened lazily on first use,
/// so the API starts even when the broker is down.
/// </summary>
public sealed class RabbitMqPublisher : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqSettings _settings;
    // Channels aren't safe for concurrent publishing, so publishes are serialized
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IOptions<RabbitMqSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>Publishes each message persistently; completes once the broker has confirmed all of them.</summary>
    /// <exception cref="MessageQueueException">The broker is unavailable or rejected a message.</exception>
    public async Task PublishAsync<T>(string routingKey, IEnumerable<T> messages,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);

            foreach (var message in messages)
            {
                var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    Type = routingKey,
                };

                // With confirmation tracking this completes once the broker has the message;
                // mandatory: true makes an unroutable message fail instead of vanishing
                await channel.BasicPublishAsync(
                    _settings.Exchange, routingKey, mandatory: true,
                    basicProperties: properties, body: body, cancellationToken: cancellationToken);
            }
        }
        catch (Exception e) when (e is BrokerUnreachableException or OperationInterruptedException
                                      or PublishException or AlreadyClosedException)
        {
            throw new MessageQueueException($"Failed to publish to {routingKey}", e);
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

            _connection = await RabbitMqConnections.CreateAsync(_settings, "gresym-api", cancellationToken);
        }

        if (_channel != null)
            await _channel.DisposeAsync();

        var options = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        _channel = await _connection.CreateChannelAsync(options, cancellationToken);

        await RabbitMqTopology.DeclareAsync(_channel, _settings, cancellationToken);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
            await _channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
        _lock.Dispose();
    }
}
