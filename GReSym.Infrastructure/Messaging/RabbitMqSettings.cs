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
    public int DeliveryLimit { get; set; } = 5;
}
