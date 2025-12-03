using System.ComponentModel.DataAnnotations.Schema;

namespace Fake.EventBus.Distributed;

/// <summary>
/// Inbox 日志条目，用于实现消费端幂等性
/// </summary>
public class InboxEventLogEntry
{
    private InboxEventLogEntry()
    {
    }

    public InboxEventLogEntry(Guid eventId, string eventTypeName, string content)
    {
        EventId = eventId;
        EventTypeName = eventTypeName;
        Content = content;
        ProcessedTime = DateTime.UtcNow;
        State = EventState.Consuming; // 初始状态为消费中
    }

    /// <summary>
    /// 事件 ID（唯一标识）
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// 事件类型名称
    /// </summary>
    public string EventTypeName { get; private set; } = default!;

    /// <summary>
    /// 处理状态
    /// </summary>
    public EventState State { get; private set; }

    /// <summary>
    /// 处理时间
    /// </summary>
    public DateTime ProcessedTime { get; private set; }

    /// <summary>
    /// 事件内容（用于日志审计）
    /// </summary>
    public string Content { get; private set; } = default!;
    
    /// <summary>
    /// 错误消息（消费失败时记录）
    /// </summary>
    public string? ErrorMessage { get; private set; }

    // 缓存计算结果，避免每次访问都执行 Split 操作
    private string? _eventTypeShortName;
    
    [NotMapped] 
    public string EventTypeShortName => _eventTypeShortName ??= EventTypeName.Split('.').Last();
    
    /// <summary>
    /// 标记为消费成功
    /// </summary>
    public void MarkAsSucceeded()
    {
        State = EventState.ConsumeSucceeded;
        ProcessedTime = DateTime.UtcNow;
    }
    
    /// <summary>
    /// 标记为消费失败
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        State = EventState.ConsumeFailed;
        ErrorMessage = errorMessage;
        ProcessedTime = DateTime.UtcNow;
    }
}
