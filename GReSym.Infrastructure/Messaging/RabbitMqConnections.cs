using RabbitMQ.Client;

namespace GReSym.Infrastructure.Messaging;

internal static class RabbitMqConnections
{
    public static Task<IConnection> CreateAsync(RabbitMqSettings settings, string clientName,
        CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            VirtualHost = settings.VirtualHost,
            UserName = settings.User,
            Password = settings.Password,
            ClientProvidedName = clientName,
            AutomaticRecoveryEnabled = true,
        };
        return factory.CreateConnectionAsync(cancellationToken);
    }
}
