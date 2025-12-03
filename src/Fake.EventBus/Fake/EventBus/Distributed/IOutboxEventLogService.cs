namespace Fake.EventBus.Distributed;

public interface IOutboxEventLogService : IDisposable
{
    Task<IEnumerable<OutboxEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId);

    /// <summary>
    /// 保存事件到 Outbox（必须在事务中）
    /// </summary>
    /// <param name="integrationEvent">集成事件</param>
    /// <param name="transactionContext">事务上下文（必需，确保业务数据和事件在同一事务）</param>
    Task SaveEventAsync(Event integrationEvent, ITransactionContext transactionContext);

    Task MarkEventAsPublishedAsync(Guid eventId);
    
    /// <summary>
    /// 标记事件为处理中（用于分布式锁，支持僵尸锁恢复）
    /// </summary>
    /// <param name="eventId">事件 ID</param>
    /// <param name="lockTimeout">锁超时时间（可选）</param>
    /// <returns>如果成功抢占到锁返回 true，否则返回 false</returns>
    Task<bool> TryMarkEventAsInProgressAsync(Guid eventId, TimeSpan? lockTimeout = null);
    
    Task MarkEventAsFailedAsync(Guid eventId);
}