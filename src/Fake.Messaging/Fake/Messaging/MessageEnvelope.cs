namespace Fake.Messaging;

/// <summary>
/// 待投递 / 已接收消息信封。
/// </summary>
public sealed class MessageEnvelope
{
    public required Guid MessageId { get; init; }

    public required string Destination { get; init; }

    public required MessageKind Kind { get; init; }

    /// <summary>UTF-8 文本体（JSON 等）。</summary>
    public required string Body { get; init; }

    public IReadOnlyDictionary<string, string> Headers { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>可选类型名（EventBus 的 EventType FullName）。</summary>
    public string? TypeName { get; init; }

    public string? TransactionId { get; init; }
}
