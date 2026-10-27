namespace Fake.Messaging;

/// <summary>由 <see cref="Message"/> + 路由解析构造待投递信封。</summary>
public static class MessageEnvelopeFactory
{
    public static MessageEnvelope Create(
        Message message,
        IMessageRouteResolver routeResolver,
        IMessageBodySerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(routeResolver);
        ArgumentNullException.ThrowIfNull(serializer);

        var route = routeResolver.Resolve(message);
        var runtimeType = message.GetType();
        var typeName = runtimeType.FullName ?? runtimeType.Name;
        var messageId = Guid.NewGuid();
        var bytes = serializer.Serialize(message, runtimeType);

        var headers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [MessageHeaders.MessageId] = messageId.ToString(),
            [MessageHeaders.Kind] = nameof(MessageKind.Rabbit),
            [MessageHeaders.EventType] = typeName,
            [MessageHeaders.EventId] = message.Id.ToString(),
            [MessageHeaders.ExchangeType] = route.TopicType
        };
        if (route.PublisherConfirm)
            headers[MessageHeaders.PublisherConfirm] = "true";

        return new MessageEnvelope
        {
            MessageId = messageId,
            Destination = route.Destination,
            Kind = MessageKind.Rabbit,
            Body = Convert.ToBase64String(bytes),
            TypeName = typeName,
            Headers = headers
        };
    }

    public static ResolvedMessageRoute ResolveRoute(Message message, IMessageRouteResolver routeResolver)
        => routeResolver.Resolve(message);
}
