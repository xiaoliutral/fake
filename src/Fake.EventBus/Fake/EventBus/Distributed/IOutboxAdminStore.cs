namespace Fake.EventBus.Distributed;

/// <summary>
/// Outbox 运维查询（Dashboard）。未接入 IntegrationEventLog 时为空实现。
/// </summary>
public interface IOutboxAdminStore
{
    Task<EventBusPagedResult<OutboxMessageDto>> QueryAsync(
        OutboxQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<OutboxMessageDto?> FindAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventStateCount>> GetStateCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 将事件重置为 <see cref="EventState.NotPublished"/>，供人工重试。
    /// </summary>
    Task ResetForRetryAsync(Guid eventId, CancellationToken cancellationToken = default);
}
