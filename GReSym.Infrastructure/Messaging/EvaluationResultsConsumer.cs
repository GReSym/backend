using System.Text.Json;
using GReSym.Core.Entities.Evaluation;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace GReSym.Infrastructure.Messaging;

/// <summary>
/// Applies the ML worker's evaluation reports ({runId, status, results?, usersEvaluated?, error?, at}) to evaluation_runs.
/// Keeps retrying the connection, so the API starts and works while the broker is down; reports wait in the queue.
/// </summary>
public sealed class EvaluationResultsConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(30);

    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EvaluationResultsConsumer> _logger;

    public EvaluationResultsConsumer(IOptions<RabbitMqSettings> settings, IServiceScopeFactory scopeFactory,
        ILogger<EvaluationResultsConsumer> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IConnection? connection = null;
            IChannel? channel = null;
            try
            {
                connection = await RabbitMqConnections.CreateAsync(_settings, "gresym-api-consumer", stoppingToken);
                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await RabbitMqTopology.DeclareAsync(channel, _settings, stoppingToken);
                await channel.BasicQosAsync(0, 1, false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                var consumerChannel = channel;
                consumer.ReceivedAsync += (_, delivery) => HandleAsync(consumerChannel, delivery, stoppingToken);
                await channel.BasicConsumeAsync(_settings.EvaluationResultsQueue, autoAck: false, consumer,
                    stoppingToken);

                _logger.LogInformation("Consuming evaluation results from {Queue}", _settings.EvaluationResultsQueue);
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
                _logger.LogWarning("Message broker unavailable ({Error}), evaluation results not consumed; retrying in {Delay}",
                    e.Message, ReconnectDelay);
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

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
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

        if (message is null || message.RunId <= 0
            || message.Status is not (EvaluationStatus.Running or EvaluationStatus.Completed or EvaluationStatus.Failed))
        {
            _logger.LogError("Malformed evaluation result {MessageId}, dead-lettering", delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var run = await db.EvaluationRuns.FirstOrDefaultAsync(r => r.Id == message.RunId, cancellationToken);

            if (run is null)
                _logger.LogWarning("Evaluation result for unknown run {RunId}, dropped", message.RunId);
            else if (Apply(run, message))
            {
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Evaluation run {RunId}: {Status}", run.Id, run.Status);
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception e) when (e is DbUpdateException or InvalidOperationException
                                      or MySqlConnector.MySqlException)
        {
            // Requeued; after x-delivery-limit redeliveries the broker dead-letters it
            _logger.LogError(e, "Saving evaluation result of run {RunId} failed", message.RunId);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }

    /// <summary>
    /// A completed run is final. Anything else may change: a transient failure is reported as failed,
    /// and the redelivered run then reports running and completed.
    /// </summary>
    private static bool Apply(EvaluationRun run, ResultMessage message)
    {
        if (run.Status == EvaluationStatus.Completed)
            return false;

        var at = message.At?.UtcDateTime ?? DateTime.UtcNow;
        run.Status = message.Status;

        switch (message.Status)
        {
            case EvaluationStatus.Running:
                run.StartedAt = at;
                run.FinishedAt = null;
                run.Error = null;
                break;
            case EvaluationStatus.Completed:
                run.StartedAt ??= at;
                run.FinishedAt = at;
                run.Error = null;
                run.Results = message.Results?.GetRawText();
                run.UsersEvaluated = message.UsersEvaluated;
                break;
            case EvaluationStatus.Failed:
                run.FinishedAt = at;
                run.Error = message.Error;
                break;
        }

        return true;
    }

    private record ResultMessage(
        int RunId,
        string Status,
        JsonElement? Results,
        int? UsersEvaluated,
        string? Error,
        DateTimeOffset? At);
}
