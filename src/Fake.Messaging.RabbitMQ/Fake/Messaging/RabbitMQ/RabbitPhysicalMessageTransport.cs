using System.Text;
using Fake.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// Destination <c>rabbit:{exchange}/{routingKey}</c> → BasicPublish。
/// </summary>
public class RabbitPhysicalMessageTransport(
    IRabbitMqConnectionPool connectionPool,
    ILogger<RabbitPhysicalMessageTransport> logger) : IPhysicalMessageTransport
{
    public bool CanPublish(string destination) => MessageDestinations.IsRabbit(destination);

    public Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var (exchange, routingKey) = MessageDestinations.ParseRabbit(envelope.Destination);
        var connectionName = envelope.Headers.GetValueOrDefault("fake-connection-name");

        using var channel = connectionPool.Get(connectionName).CreateModel();
        channel.ExchangeDeclare(exchange, ExchangeType.Direct, durable: true);

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        properties.MessageId = envelope.MessageId.ToString();
        properties.Headers ??= new Dictionary<string, object>();
        foreach (var (k, v) in envelope.Headers)
            properties.Headers[k] = v;

        var body = DecodeBody(envelope.Body);

        logger.LogDebug(
            "Publishing rabbit message {MessageId} to {Exchange}/{RoutingKey}",
            envelope.MessageId, exchange, routingKey);

        channel.BasicPublish(exchange, routingKey, mandatory: true, properties, body);
        return Task.CompletedTask;
    }

    private static byte[] DecodeBody(string body)
    {
        try
        {
            return Convert.FromBase64String(body);
        }
        catch (FormatException)
        {
            return Encoding.UTF8.GetBytes(body);
        }
    }
}
