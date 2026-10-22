using Fake.Messaging;

namespace Fake.Messaging.Admin;

public sealed class NullMessageInboxAdminStore : IMessageInboxAdminStore
{
    public static NullMessageInboxAdminStore Instance { get; } = new();

    public Task<MessagingPagedResult<InboxMessageAdminDto>> QueryAsync(
        MessageInboxQueryRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new MessagingPagedResult<InboxMessageAdminDto>(
            Array.Empty<InboxMessageAdminDto>(), 0, request.PageIndex, request.PageSize));

    public Task<InboxMessageAdminDto?> FindAsync(
        Guid messageId,
        string? consumerGroup = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<InboxMessageAdminDto?>(null);

    public Task<IReadOnlyList<InboxStateCount>> GetStateCountsAsync(
        MessageKind? kind = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InboxStateCount>>(Array.Empty<InboxStateCount>());

    public Task ResetForRetryAsync(
        Guid messageId,
        string? consumerGroup = null,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Messaging Inbox admin store is not configured.");

    public Task<bool> TryResetForRetryAsync(
        Guid messageId,
        string? consumerGroup = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
