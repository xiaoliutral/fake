using System.Text.Json;
using Fake.EventBus;
using Fake.EventBus.Distributed;
using Fake.Messaging;

namespace Fake.EventBus.Messaging;

/// <summary>
/// 将 IntegrationEvent 转为 Messaging Envelope（Destination=eventbus:*）。
/// </summary>
public static class EventBusMessageFactory
{
    public static MessageEnvelope Create(
        Event @event,
        IEventRoutingKeyResolver routingKeyResolver,
        string? transactionId = null)
    {
        var eventType = @event.GetType();
        var routingKey = routingKeyResolver.Resolve(eventType);
        var body = JsonSerializer.Serialize(@event, eventType);

        return new MessageEnvelope
        {
            MessageId = @event.Id,
            Destination = MessageDestinations.ForEventBus(routingKey),
            Kind = MessageKind.EventBus,
            Body = body,
            TypeName = eventType.FullName,
            TransactionId = transactionId,
            Headers = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [MessageHeaders.EventId] = @event.Id.ToString(),
                [MessageHeaders.EventType] = eventType.FullName ?? eventType.Name,
                [MessageHeaders.Kind] = nameof(MessageKind.EventBus)
            }
        };
    }

    public static MessageEnvelope CreateInboxEnvelope(Guid eventId, string eventTypeName, string content)
        => new()
        {
            MessageId = eventId,
            Destination = MessageDestinations.ForEventBus(eventTypeName),
            Kind = MessageKind.EventBus,
            Body = content,
            TypeName = eventTypeName,
            Headers = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [MessageHeaders.EventId] = eventId.ToString(),
                [MessageHeaders.EventType] = eventTypeName,
                [MessageHeaders.Kind] = nameof(MessageKind.EventBus)
            }
        };

    public static string ShortTypeName(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return string.Empty;
        var i = typeName.LastIndexOf('.');
        return i < 0 ? typeName : typeName[(i + 1)..];
    }
}
