using System.Text.Json;

namespace Fake.Messaging.EntityFrameworkCore;

public class MessageOutboxEntry
{
    private MessageOutboxEntry()
    {
    }

    public MessageOutboxEntry(MessageEnvelope envelope)
    {
        MessageId = envelope.MessageId;
        Destination = envelope.Destination;
        Kind = envelope.Kind;
        Body = envelope.Body;
        TypeName = envelope.TypeName;
        TransactionId = envelope.TransactionId ?? string.Empty;
        HeadersJson = SerializeHeaders(envelope.Headers);
        State = MessageOutboxState.NotPublished;
        TimesSent = 0;
        CreationTime = DateTime.UtcNow;
    }

    public Guid MessageId { get; private set; }
    public string Destination { get; private set; } = default!;
    public MessageKind Kind { get; private set; }
    public string Body { get; private set; } = default!;
    public string? TypeName { get; private set; }
    public string TransactionId { get; private set; } = default!;
    public string HeadersJson { get; private set; } = "{}";
    public MessageOutboxState State { get; private set; }
    public int TimesSent { get; private set; }
    public DateTime CreationTime { get; private set; }
    public DateTime? LockExpiresAt { get; private set; }
    public string? LastError { get; private set; }

    public IReadOnlyDictionary<string, string> GetHeaders()
        => DeserializeHeaders(HeadersJson);

    public void UpdateState(MessageOutboxState state, string? error = null)
    {
        State = state;
        if (error != null)
            LastError = error;
        if (state is MessageOutboxState.Published or MessageOutboxState.NotPublished)
            LockExpiresAt = null;
    }

    public void MarkInProgress(DateTime? lockExpiresAt)
    {
        State = MessageOutboxState.InProgress;
        LockExpiresAt = lockExpiresAt;
        TimesSent++;
    }

    public void ResetForRetry()
    {
        State = MessageOutboxState.NotPublished;
        LockExpiresAt = null;
        LastError = null;
    }

    public MessageOutboxRecord ToRecord() => new()
    {
        MessageId = MessageId,
        Destination = Destination,
        Kind = Kind,
        Body = Body,
        State = State,
        TimesSent = TimesSent,
        CreationTime = CreationTime,
        TransactionId = TransactionId,
        TypeName = TypeName,
        Headers = GetHeaders(),
        LockExpiresAt = LockExpiresAt,
        LastError = LastError
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
