using Fake.Messaging;

namespace Fake.Messaging.Admin;

/// <summary>
/// Outbox 运维查询（Dashboard）。未接入 Messaging.EF 时为空实现。
/// </summary>
public interface IMessageOutboxAdminStore
{
    Task<MessagingPagedResult<OutboxMessageAdminDto>> QueryAsync(
        MessageOutboxQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<OutboxMessageAdminDto?> FindAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxStateCount>> GetStateCountsAsync(
        MessageKind? kind = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 将消息重置为 <see cref="MessageOutboxState.NotPublished"/>，供人工重试。
    /// </summary>
    Task ResetForRetryAsync(Guid messageId, CancellationToken cancellationToken = default);
}
