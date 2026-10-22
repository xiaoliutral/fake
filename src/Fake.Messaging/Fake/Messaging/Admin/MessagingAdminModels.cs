using Fake.Messaging;

namespace Fake.Messaging.Admin;

public sealed record MessagingPagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int PageIndex,
    int PageSize);

public sealed record OutboxMessageAdminDto(
    Guid MessageId,
    string Destination,
    MessageKind Kind,
    string? TypeName,
    string TypeShortName,
    MessageOutboxState State,
    int TimesSent,
    DateTime CreationTime,
    string TransactionId,
    string? Body,
    DateTime? LockExpiresAt,
    string? LastError);

public sealed record InboxMessageAdminDto(
    Guid MessageId,
    string ConsumerGroup,
    MessageKind Kind,
    string? Destination,
    string? TypeName,
    string TypeShortName,
    MessageInboxState State,
    DateTime ProcessedTime,
    string? ErrorMessage,
    string? Body);

public sealed record OutboxStateCount(MessageOutboxState State, long Count);

public sealed record InboxStateCount(MessageInboxState State, long Count);

public sealed class MessageOutboxQueryRequest
{
    public MessageOutboxState? State { get; set; }
    public MessageKind? Kind { get; set; }
    public string? Destination { get; set; }
    public string? TypeName { get; set; }

    /// <summary>
    /// 统一检索：MessageId（完整 GUID 精确匹配，否则模糊）/ TransactionId 包含。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>消息体 Body 包含。</summary>
    public string? Content { get; set; }

    public Guid? MessageId { get; set; }
    public string? TransactionId { get; set; }

    /// <summary>CreationTime 下界（含，UTC）。</summary>
    public DateTime? From { get; set; }

    /// <summary>CreationTime 上界（含，UTC）。</summary>
    public DateTime? To { get; set; }

    public int? MinTimesSent { get; set; }
    public int? MaxTimesSent { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class MessageInboxQueryRequest
{
    public MessageInboxState? State { get; set; }
    public MessageKind? Kind { get; set; }
    public string? ConsumerGroup { get; set; }
    public string? Destination { get; set; }
    public string? TypeName { get; set; }

    /// <summary>
    /// 统一检索：MessageId（完整 GUID 精确匹配，否则模糊）/ ErrorMessage 包含。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>消息体 Body 包含。</summary>
    public string? Content { get; set; }

    public Guid? MessageId { get; set; }

    /// <summary>ErrorMessage 包含。</summary>
    public string? Error { get; set; }

    /// <summary>ProcessedTime 下界（含，UTC）。</summary>
    public DateTime? From { get; set; }

    /// <summary>ProcessedTime 上界（含，UTC）。</summary>
    public DateTime? To { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class MessageOutboxRetryRequest
{
    /// <summary>
    /// true：重置为 NotPublished 后立即 Dispatch；false：仅改状态，等待后台扫描。
    /// </summary>
    public bool DispatchNow { get; set; } = true;

    /// <summary>兼容旧 Dashboard 字段名 <c>publishNow</c>。</summary>
    public bool? PublishNow
    {
        get => DispatchNow;
        set
        {
            if (value.HasValue)
                DispatchNow = value.Value;
        }
    }

    /// <summary>
    /// true（默认）：同步清除 Inbox 幂等记录，否则消费端会把重投当成重复消息直接跳过。
    /// </summary>
    public bool ClearInbox { get; set; } = true;
}

public sealed class MessageInboxRetryRequest
{
    /// <summary>
    /// true（默认）：若已注册本地 Reprocess 适配则立即执行；false：仅重置 Inbox 状态。
    /// </summary>
    public bool ReprocessNow { get; set; } = true;
}

public sealed record MessageDispatchOneResult(bool Dispatched, string? Error = null, MessageOutboxState? State = null);

public sealed record MessageInboxReprocessResult(bool Processed, string? Error = null, MessageInboxState? State = null);

public sealed record MessagingDlxMessageDto(
    string MessageId,
    string? RoutingKey,
    string? Exchange,
    string? RelatedMessageId,
    string? TypeName,
    string Body,
    IReadOnlyDictionary<string, string> Headers,
    DateTime? DeathTime,
    string? DeathReason);

public sealed record MessagingSubscriptionDto(
    string TypeName,
    string TypeShortName,
    string RoutingKey,
    IReadOnlyList<string> Handlers);
