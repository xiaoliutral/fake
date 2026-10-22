namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// 独立队列发布信封便捷构造。
/// </summary>
public sealed class RabbitEnvelope
{
    public required string Exchange { get; init; }
    public required string RoutingKey { get; init; }
    public required ReadOnlyMemory<byte> Body { get; init; }
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
    public string? TypeName { get; init; }
    public string? ConnectionName { get; init; }

    public MessageEnvelope ToMessageEnvelope()
    {
        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        if (Headers != null)
        {
            foreach (var (k, v) in Headers)
                headers[k] = v;
        }

        headers[MessageHeaders.MessageId] = MessageId.ToString();
        headers[MessageHeaders.Kind] = nameof(MessageKind.Rabbit);

        return new MessageEnvelope
        {
            MessageId = MessageId,
            Destination = MessageDestinations.ForRabbit(Exchange, RoutingKey),
            Kind = MessageKind.Rabbit,
            Body = Convert.ToBase64String(Body.Span),
            TypeName = TypeName,
            Headers = headers
        };
    }
}
