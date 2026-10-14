namespace Fake.EventBus.Distributed;

public sealed class NullInboxAdminStore : IInboxAdminStore
{
    public static NullInboxAdminStore Instance { get; } = new();

    public Task<EventBusPagedResult<InboxMessageDto>> QueryAsync(
        InboxQueryRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new EventBusPagedResult<InboxMessageDto>(
            Array.Empty<InboxMessageDto>(), 0, request.PageIndex, request.PageSize));

    public Task<InboxMessageDto?> FindAsync(Guid eventId, CancellationToken cancellationToken = default)
        => Task.FromResult<InboxMessageDto?>(null);

    public Task<IReadOnlyList<EventStateCount>> GetStateCountsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EventStateCount>>(Array.Empty<EventStateCount>());

    public Task ResetForRetryAsync(Guid eventId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Inbox admin store is not available. Add Fake.EventBus.IntegrationEventLog.");

    public Task<bool> TryResetForRetryAsync(Guid eventId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
