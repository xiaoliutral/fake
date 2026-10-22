namespace Fake.Messaging;

/// <summary>
/// Outbox 存储查询扩展（Dispatcher / Admin 使用）。
/// </summary>
public interface IMessageOutboxStore : IMessageOutbox
{
    /// <summary>
    /// 征用调用方事务后写入 Outbox。
    /// </summary>
    Task EnqueueInTransactionAsync(
        MessageEnvelope envelope,
        object underlyingTransaction,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageOutboxRecord>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageOutboxRecord>> GetPendingByTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken = default);

    Task<MessageOutboxRecord?> FindAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task<bool> TryMarkInProgressAsync(Guid messageId, TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default);

    Task ResetForRetryAsync(Guid messageId, CancellationToken cancellationToken = default);
}

public sealed class MessageOutboxRecord
{
    public required Guid MessageId { get; init; }
    public required string Destination { get; init; }
    public required MessageKind Kind { get; init; }
    public required string Body { get; init; }
    public required MessageOutboxState State { get; init; }
    public required int TimesSent { get; init; }
    public required DateTime CreationTime { get; init; }
    public required string TransactionId { get; init; }
    public string? TypeName { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);
    public DateTime? LockExpiresAt { get; init; }
    public string? LastError { get; init; }

    public MessageEnvelope ToEnvelope() => new()
    {
        MessageId = MessageId,
        Destination = Destination,
        Kind = Kind,
        Body = Body,
        TypeName = TypeName,
        TransactionId = TransactionId,
        Headers = Headers
    };
}
