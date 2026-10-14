namespace Fake.EventBus.Distributed;

/// <summary>
/// Inbox 运维查询（Dashboard）。未接入 IntegrationEventLog 时为空实现。
/// </summary>
public interface IInboxAdminStore
{
    Task<EventBusPagedResult<InboxMessageDto>> QueryAsync(
        InboxQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<InboxMessageDto?> FindAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventStateCount>> GetStateCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置为可重新抢占状态（ConsumeFailed），配合 Outbox 重投 / DLX Requeue 完成人工重试。
    /// </summary>
    Task ResetForRetryAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 若存在 Inbox 记录则重置并返回 true；不存在返回 false（不抛异常）。
    /// </summary>
    Task<bool> TryResetForRetryAsync(Guid eventId, CancellationToken cancellationToken = default);
}
