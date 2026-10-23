using System.Text;
using Fake.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// Destination <c>rabbit:{exchange}/{routingKey}</c> → BasicPublish（通道池；支持 Topic/Confirm）。
/// </summary>
public class RabbitPhysicalMessageTransport(
    IRabbitMqChannelPool channelPool,
    ILogger<RabbitPhysicalMessageTransport> logger) : IPhysicalMessageTransport
{
    public bool CanPublish(string destination) => MessageDestinations.IsRabbit(destination);

    public Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var (exchange, routingKey) = MessageDestinations.ParseRabbit(envelope.Destination);
        var connectionName = envelope.Headers.GetValueOrDefault("fake-connection-name");
        var exchangeType = envelope.Headers.GetValueOrDefault(MessageHeaders.ExchangeType)
                           ?? ExchangeType.Direct;
        var confirm = string.Equals(
            envelope.Headers.GetValueOrDefault(MessageHeaders.PublisherConfirm),
            "true",
            StringComparison.OrdinalIgnoreCase);

        using var accessor = channelPool.Acquire(
            channelName: $"msg:{exchange}/{routingKey}",
            connectionName: connectionName);
        var channel = accessor.Channel;
        channel.ExchangeDeclare(exchange, exchangeType, durable: true);

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        properties.MessageId = envelope.MessageId.ToString();
        properties.Headers ??= new Dictionary<string, object>();
        foreach (var (k, v) in envelope.Headers)
            properties.Headers[k] = v;

        var body = DecodeBody(envelope.Body);

        if (confirm)
            channel.ConfirmSelect();

        logger.LogDebug(
            "Publishing rabbit message {MessageId} to {Exchange}/{RoutingKey} (type={ExchangeType}, confirm={Confirm})",
            envelope.MessageId, exchange, routingKey, exchangeType, confirm);

        channel.BasicPublish(exchange, routingKey, mandatory: true, properties, body);

        if (confirm)
            channel.WaitForConfirmsOrDie();

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
