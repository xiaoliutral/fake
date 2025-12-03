namespace Fake.EventBus.Distributed;

/// <summary>
/// Inbox 事件日志服务接口，用于实现消费端幂等性
/// </summary>
public interface IInboxEventLogService : IDisposable
{
    /// <summary>
    /// 检查事件是否已处理过
    /// </summary>
    Task<bool> IsEventProcessedAsync(Guid eventId);

    /// <summary>
    /// 保存已处理的事件到 Inbox
    /// </summary>
    Task SaveProcessedEventAsync(Guid eventId, string eventTypeName, string content);
    
    /// <summary>
    /// 原子性地尝试标记事件为处理中
    /// </summary>
    /// <returns>如果成功插入返回 true，如果已存在返回 false</returns>
    Task<bool> TryMarkAsProcessingAsync(Guid eventId, string eventTypeName, string content);
    
    /// <summary>
    /// 标记事件处理成功
    /// </summary>
    Task MarkAsSucceededAsync(Guid eventId);
    
    /// <summary>
    /// 标记事件处理失败
    /// </summary>
    Task MarkAsFailedAsync(Guid eventId, string errorMessage);
}
