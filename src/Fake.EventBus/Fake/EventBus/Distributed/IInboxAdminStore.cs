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
    /// 重置为可重新抢占状态（ConsumeFailed），配合 DLX Requeue 完成人工重试。
    /// </summary>
    Task ResetForRetryAsync(Guid eventId, CancellationToken cancellationToken = default);
}
