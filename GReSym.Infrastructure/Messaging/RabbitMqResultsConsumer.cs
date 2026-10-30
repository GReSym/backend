using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace GReSym.Infrastructure.Messaging;

/// <summary>
/// Consumes one queue of reports from the ML worker, one message at a time.
/// Keeps retrying the connection, so the API starts and works while the broker is down; reports wait in the queue.
/// </summary>
public abstract class RabbitMqResultsConsumer : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(30);

    private readonly ILogger _logger;

    protected RabbitMqResultsConsumer(RabbitMqSettings settings, ILogger logger)
    {
        Settings = settings;
        _logger = logger;
    }

    protected RabbitMqSettings Settings { get; }

    protected abstract string QueueName { get; }

    /// <summary>Must ack or nack the delivery.</summary>
    protected abstract Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IConnection? connection = null;
            IChannel? channel = null;
            try
            {
                connection = await RabbitMqConnections.CreateAsync(Settings, $"gresym-api-consumer ({QueueName})",
                    stoppingToken);
                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await RabbitMqTopology.DeclareAsync(channel, Settings, stoppingToken);
                await channel.BasicQosAsync(0, 1, false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                var consumerChannel = channel;
                consumer.ReceivedAsync += (_, delivery) => HandleAsync(consumerChannel, delivery, stoppingToken);
                await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);

                _logger.LogInformation("Consuming {Queue}", QueueName);
                // Automatic recovery restores the connection and the consumer after broker restarts
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e) when (e is BrokerUnreachableException or OperationInterruptedException
                                          or AlreadyClosedException)
            {
                _logger.LogWarning("Message broker unavailable ({Error}), {Queue} not consumed; retrying in {Delay}",
                    e.Message, QueueName, ReconnectDelay);
            }
            finally
            {
                if (channel != null)
                    await channel.DisposeAsync();
                if (connection != null)
                    await connection.DisposeAsync();
            }

            try
            {
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
