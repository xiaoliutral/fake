using Fake.Messaging;

namespace Fake.Messaging.Admin;

public sealed class NullMessageOutboxAdminStore : IMessageOutboxAdminStore
{
    public static NullMessageOutboxAdminStore Instance { get; } = new();

    public Task<MessagingPagedResult<OutboxMessageAdminDto>> QueryAsync(
        MessageOutboxQueryRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new MessagingPagedResult<OutboxMessageAdminDto>(
            Array.Empty<OutboxMessageAdminDto>(), 0, request.PageIndex, request.PageSize));

    public Task<OutboxMessageAdminDto?> FindAsync(Guid messageId, CancellationToken cancellationToken = default)
        => Task.FromResult<OutboxMessageAdminDto?>(null);

    public Task<IReadOnlyList<OutboxStateCount>> GetStateCountsAsync(
        MessageKind? kind = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OutboxStateCount>>(Array.Empty<OutboxStateCount>());

    public Task ResetForRetryAsync(Guid messageId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Messaging Outbox admin store is not configured.");
}
