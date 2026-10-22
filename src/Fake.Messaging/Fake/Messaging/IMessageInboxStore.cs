namespace Fake.Messaging;

/// <summary>
/// Inbox 存储扩展（Admin / 清理）。
/// </summary>
public interface IMessageInboxStore : IMessageInbox
{
    Task ResetForRetryAsync(Guid messageId, string consumerGroup, CancellationToken cancellationToken = default);

    Task<MessageInboxRecord?> FindAsync(
        Guid messageId,
        string consumerGroup,
        CancellationToken cancellationToken = default);
}

public sealed class MessageInboxRecord
{
    public required Guid MessageId { get; init; }
    public required string ConsumerGroup { get; init; }
    public required MessageKind Kind { get; init; }
    public required string Body { get; init; }
    public required MessageInboxState State { get; init; }
    public required DateTime ProcessedTime { get; init; }
    public string? TypeName { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Destination { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);
}
