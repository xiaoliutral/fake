namespace Fake.EventBus.Distributed;

public sealed record EventBusPagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int PageIndex,
    int PageSize);

public sealed record OutboxMessageDto(
    Guid EventId,
    string EventTypeName,
    string EventTypeShortName,
    EventState State,
    int TimesSent,
    DateTime CreationTime,
    string TransactionId,
    string? Content,
    DateTime? LockExpiresAt);

public sealed record InboxMessageDto(
    Guid EventId,
    string EventTypeName,
    string EventTypeShortName,
    EventState State,
    DateTime ProcessedTime,
    string? ErrorMessage,
    string? Content);

public sealed record EventStateCount(EventState State, long Count);

public sealed record DlxMessageDto(
    string MessageId,
    string? RoutingKey,
    string? Exchange,
    string? EventId,
    string? EventType,
    string Body,
    IReadOnlyDictionary<string, string> Headers,
    DateTime? DeathTime,
    string? DeathReason);

public sealed class OutboxQueryRequest
{
    public EventState? State { get; set; }
    public string? EventType { get; set; }

    /// <summary>
    /// 统一检索：EventId（完整 GUID 精确匹配，否则模糊）/ TransactionId 包含。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>消息体 Content 包含（大小写敏感，取决于数据库 collation）。</summary>
    public string? Content { get; set; }

    /// <summary>精确 EventId（可选；Dashboard 已并入 Keyword）。</summary>
    public Guid? EventId { get; set; }

    /// <summary>TransactionId 包含（可选；Dashboard 已并入 Keyword）。</summary>
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

public sealed class InboxQueryRequest
{
    public EventState? State { get; set; }
    public string? EventType { get; set; }

    /// <summary>
    /// 统一检索：EventId（完整 GUID 精确匹配，否则模糊）/ ErrorMessage 包含。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>消息体 Content 包含。</summary>
    public string? Content { get; set; }

    /// <summary>精确 EventId（可选；Dashboard 已并入 Keyword）。</summary>
    public Guid? EventId { get; set; }

    /// <summary>ErrorMessage 包含（可选；Dashboard 已并入 Keyword）。</summary>
    public string? Error { get; set; }

    /// <summary>ProcessedTime 下界（含，UTC）。</summary>
    public DateTime? From { get; set; }

    /// <summary>ProcessedTime 上界（含，UTC）。</summary>
    public DateTime? To { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class OutboxRetryRequest
{
    /// <summary>
    /// true：重置为 NotPublished 后立即 Publish；false：仅改状态，等待后台扫描。
    /// </summary>
    public bool PublishNow { get; set; } = true;

    /// <summary>
    /// true（默认）：同步清除 Inbox 幂等记录，否则消费端会把重投当成重复消息直接跳过。
    /// </summary>
    public bool ClearInbox { get; set; } = true;
}

public sealed class InboxRetryRequest
{
    /// <summary>
    /// true（默认）：本机重新执行已注册 Handler；false：仅重置 Inbox 状态。
    /// </summary>
    public bool ReprocessNow { get; set; } = true;
}

public sealed record OutboxPublishOneResult(bool Published, string? Error = null, EventState? State = null);

public sealed record InboxReprocessResult(bool Processed, string? Error = null, EventState? State = null);

/// <summary>
/// 已注册的集成事件及其消费者（Handler）。
/// </summary>
public sealed record EventSubscriptionDto(
    string EventTypeName,
    string EventTypeShortName,
    string RoutingKey,
    IReadOnlyList<string> Handlers);
