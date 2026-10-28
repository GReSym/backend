namespace GReSym.Infrastructure.Messaging;

/// <summary>"RabbitMq" section of appsettings. Names and arguments must match ml/config.example.json.</summary>
public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string Exchange { get; set; } = "gresym.tasks";
    public string DeadLetterExchange { get; set; } = "gresym.dlx";
    public string VectorizeQueue { get; set; } = "ml.vectorize-game";
    public string VectorizeDeadLetterQueue { get; set; } = "ml.vectorize-game.dlq";
    public string VectorizeRoutingKey { get; set; } = "game.vectorize";
    /// <summary>Also used for the evaluation results queue.</summary>
    public int DeliveryLimit { get; set; } = 5;

    // Offline evaluation: API → EvaluationQueue → ML worker → EvaluationResultsQueue → API
    public string EvaluationQueue { get; set; } = "ml.evaluate";
    public string EvaluationDeadLetterQueue { get; set; } = "ml.evaluate.dlq";
    public string EvaluationRoutingKey { get; set; } = "evaluation.run";
    public int EvaluationDeliveryLimit { get; set; } = 2;
    public string EvaluationResultsQueue { get; set; } = "api.evaluation-results";
    public string EvaluationResultsDeadLetterQueue { get; set; } = "api.evaluation-results.dlq";
    public string EvaluationResultsRoutingKey { get; set; } = "evaluation.result";
}
