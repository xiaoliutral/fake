namespace Fake.EventBus.Distributed;

/// <summary>
/// Inbox 事件日志服务接口，用于实现消费端幂等性
/// </summary>
public interface IInboxEventLogService
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
    /// 原子性地尝试标记事件为处理中（含僵尸 Consuming / ConsumeFailed / NoHandler 抢占）。
    /// Consuming 超过 <c>IntegrationEventLog:Inbox:ProcessingLockTimeout</c> 可抢占；
    /// <paramref name="forceReclaim"/> 为 true（通常对应 RabbitMQ Redelivered）时，
    /// 即使未超时也可抢占 Consuming——因为原消费者已丢失投递。
    /// </summary>
    /// <returns>成功插入或抢占返回 true；已 ConsumeSucceeded（或锁未过期且未强制抢占）返回 false</returns>
    Task<bool> TryMarkAsProcessingAsync(
        Guid eventId,
        string eventTypeName,
        string content,
        bool forceReclaim = false);

    /// <summary>
    /// 标记事件处理成功
    /// </summary>
    Task MarkAsSucceededAsync(Guid eventId);

    /// <summary>
    /// 标记事件处理失败
    /// </summary>
    Task MarkAsFailedAsync(Guid eventId, string errorMessage);

    /// <summary>
    /// 标记为无匹配 Handler（终态偏审计；重投时可再抢占）
    /// </summary>
    Task MarkAsNoHandlerAsync(Guid eventId, string? message = null);

    /// <summary>
    /// 人工重试：重置为可重新抢占状态
    /// </summary>
    Task ResetEventForRetryAsync(Guid eventId, CancellationToken cancellationToken = default);
}
