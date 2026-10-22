using System.Text.Json;

namespace Fake.Messaging.EntityFrameworkCore;

public class MessageInboxEntry
{
    private MessageInboxEntry()
    {
    }

    public MessageInboxEntry(Guid messageId, string consumerGroup, MessageEnvelope envelope)
    {
        MessageId = messageId;
        ConsumerGroup = consumerGroup;
        Kind = envelope.Kind;
        Body = envelope.Body;
        TypeName = envelope.TypeName;
        Destination = envelope.Destination;
        HeadersJson = SerializeHeaders(envelope.Headers);
        State = MessageInboxState.Consuming;
        ProcessedTime = DateTime.UtcNow;
    }

    public Guid MessageId { get; private set; }
    public string ConsumerGroup { get; private set; } = default!;
    public MessageKind Kind { get; private set; }
    public string Body { get; private set; } = default!;
    public string? TypeName { get; private set; }
    public string? Destination { get; private set; }
    public string HeadersJson { get; private set; } = "{}";
    public MessageInboxState State { get; private set; }
    public DateTime ProcessedTime { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IReadOnlyDictionary<string, string> GetHeaders()
        => DeserializeHeaders(HeadersJson);

    public void MarkAsConsuming()
    {
        State = MessageInboxState.Consuming;
        ErrorMessage = null;
        ProcessedTime = DateTime.UtcNow;
    }

    public void MarkAsSucceeded()
    {
        State = MessageInboxState.ConsumeSucceeded;
        ProcessedTime = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        State = MessageInboxState.ConsumeFailed;
        ErrorMessage = errorMessage;
        ProcessedTime = DateTime.UtcNow;
    }

    public void MarkAsNoHandler(string? message = null)
    {
        State = MessageInboxState.NoHandler;
        ErrorMessage = message ?? "No matching handler";
        ProcessedTime = DateTime.UtcNow;
    }

    public void ResetForRetry(string? reason = null)
    {
        State = MessageInboxState.ConsumeFailed;
        ErrorMessage = reason ?? "Reset for manual retry";
        ProcessedTime = DateTime.UtcNow;
    }

    public MessageInboxRecord ToRecord() => new()
    {
        MessageId = MessageId,
        ConsumerGroup = ConsumerGroup,
        Kind = Kind,
        Body = Body,
        State = State,
        ProcessedTime = ProcessedTime,
        TypeName = TypeName,
        ErrorMessage = ErrorMessage,
        Destination = Destination,
        Headers = GetHeaders()
    };

    private static string SerializeHeaders(IReadOnlyDictionary<string, string>? headers)
    {
        if (headers == null || headers.Count == 0)
            return "{}";
        return JsonSerializer.Serialize(headers);
    }

    private static IReadOnlyDictionary<string, string> DeserializeHeaders(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
            return new Dictionary<string, string>(StringComparer.Ordinal);

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
