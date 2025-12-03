using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Fake.EventBus.Distributed;

/// <summary>
/// Outbox 事件日志条目（发送端最终一致性）
/// </summary>
public class OutboxEventLogEntry
{
    private OutboxEventLogEntry()
    {
    }

    public OutboxEventLogEntry(Event @event, Guid transactionId)
    {
        EventId = @event.Id;
        CreationTime = @event.CreationTime;
        EventTypeName = @event.GetType().FullName ?? String.Empty;
        Content = JsonSerializer.Serialize(@event);
        State = EventState.NotPublished;
        TimesSent = 0;
        TransactionId = transactionId.ToString();
    }

    public OutboxEventLogEntry(string transactionId)
    {
        TransactionId = transactionId;
    }

    public Guid EventId { get; private set; }

    public string EventTypeName { get; private set; } = default!;

    /// <summary>
    /// 事件状态
    /// </summary>
    public EventState State { get; private set; }

    /// <summary>
    /// 发送次数
    /// </summary>
    public int TimesSent { get; private set; }

    /// <summary>
    /// 事件创建时间
    /// </summary>
    public DateTime CreationTime { get; private set; }

    /// <summary>
    /// 发送内容
    /// </summary>
    public string Content { get; private set; } = default!;

    /// <summary>
    /// 事务Id
    /// </summary>
    public string TransactionId { get; private set; } = default!;

    /// <summary>
    /// 锁过期时间（用于僵尸锁恢复）
    /// </summary>
    public DateTime? LockExpiresAt { get; private set; }

    // 缓存计算结果，避免每次访问都执行 Split 操作
    private string? _eventTypeShortName;
    
    [NotMapped] 
    public string EventTypeShortName => _eventTypeShortName ??= EventTypeName.Split('.').Last();
    
    [NotMapped] 
    public Event? IntegrationEvent { get; private set; }


    public OutboxEventLogEntry DeserializeJsonContent(Type type)
    {
        IntegrationEvent = JsonSerializer.Deserialize(Content, type)?.As<Event>();
        return this;
    }

    public void UpdateEventStatus(EventState status)
    {
        State = status;
    }

    public void TimesSentIncr(int value = 1)
    {
        TimesSent += value;
    }
}
