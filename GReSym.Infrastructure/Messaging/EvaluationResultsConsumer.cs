using System.Text.Json;
using GReSym.Core.Entities.Evaluation;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GReSym.Infrastructure.Messaging;

/// <summary>
/// Applies the ML worker's evaluation reports ({runId, status, results?, usersEvaluated?, error?, at}) to evaluation_runs.
/// </summary>
public sealed class EvaluationResultsConsumer : RabbitMqResultsConsumer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EvaluationResultsConsumer> _logger;

    public EvaluationResultsConsumer(IOptions<RabbitMqSettings> settings, IServiceScopeFactory scopeFactory,
        ILogger<EvaluationResultsConsumer> logger)
        : base(settings.Value, logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override string QueueName => Settings.EvaluationResultsQueue;

    protected override async Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
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
