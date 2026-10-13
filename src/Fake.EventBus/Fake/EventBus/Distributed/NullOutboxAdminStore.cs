namespace Fake.EventBus.Distributed;

public sealed class NullOutboxAdminStore : IOutboxAdminStore
{
    public static NullOutboxAdminStore Instance { get; } = new();

    public Task<EventBusPagedResult<OutboxMessageDto>> QueryAsync(
        OutboxQueryRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new EventBusPagedResult<OutboxMessageDto>(
            Array.Empty<OutboxMessageDto>(), 0, request.PageIndex, request.PageSize));

    public Task<OutboxMessageDto?> FindAsync(Guid eventId, CancellationToken cancellationToken = default)
        => Task.FromResult<OutboxMessageDto?>(null);

    public Task<IReadOnlyList<EventStateCount>> GetStateCountsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EventStateCount>>(Array.Empty<EventStateCount>());

    public Task ResetForRetryAsync(Guid eventId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Outbox admin store is not available. Add Fake.EventBus.IntegrationEventLog.");
}
