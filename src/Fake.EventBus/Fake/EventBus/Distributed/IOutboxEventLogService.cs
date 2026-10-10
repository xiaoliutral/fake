namespace Fake.EventBus.Distributed;

public interface IOutboxEventLogService
{
    /// <summary>
    /// 按事务 Id 拉取尚未发布的 Outbox 事件（事务提交后立即发布场景）
    /// </summary>
    Task<IEnumerable<OutboxEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId);

    /// <summary>
    /// 拉取全局待发布事件（后台扫描：NotPublished / PublishFailed）
    /// </summary>
    Task<IEnumerable<OutboxEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(
        int maxCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存事件到 Outbox（必须在事务中）
    /// </summary>
    Task SaveEventAsync(Event integrationEvent, ITransactionContext transactionContext);

    Task MarkEventAsPublishedAsync(Guid eventId);

    /// <summary>
    /// 标记事件为处理中（分布式锁，支持僵尸锁 / 失败重试抢占）
    /// </summary>
    Task<bool> TryMarkEventAsInProgressAsync(Guid eventId, TimeSpan? lockTimeout = null);

    Task MarkEventAsFailedAsync(Guid eventId);
}
