using System.Text.Json;
using Fake.EventBus.Distributed;
using Fake.Messaging;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.Messaging;

/// <summary>
/// Destination <c>eventbus:*</c> → <see cref="IPhysicalDistributedEventBus"/>.
/// </summary>
public class EventBusPhysicalMessageTransport(
    IPhysicalDistributedEventBus physicalBus,
    ILogger<EventBusPhysicalMessageTransport> logger) : IPhysicalMessageTransport
{
    public bool CanPublish(string destination) => MessageDestinations.IsEventBus(destination);

    public async Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(envelope.TypeName))
            throw new InvalidOperationException($"EventBus envelope {envelope.MessageId} missing TypeName.");

        var eventType = IntegrationEventTypeResolver.Resolve(envelope.TypeName);
        var @event = JsonSerializer.Deserialize(envelope.Body, eventType) as IntegrationEvent
                     ?? throw new InvalidOperationException(
                         $"Failed to deserialize EventBus message {envelope.MessageId} as {envelope.TypeName}.");

        logger.LogDebug(
            "Publishing EventBus message {MessageId} ({EventType}) via physical bus",
            envelope.MessageId, eventType.Name);

        await physicalBus.PublishAsync(@event, cancellationToken);
    }
}
