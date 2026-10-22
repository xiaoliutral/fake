using Fake.Messaging;

namespace Fake.Messaging.Admin;

/// <summary>
/// Inbox 运维查询（Dashboard）。未接入 Messaging.EF 时为空实现。
/// </summary>
public interface IMessageInboxAdminStore
{
    Task<MessagingPagedResult<InboxMessageAdminDto>> QueryAsync(
        MessageInboxQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<InboxMessageAdminDto?> FindAsync(
        Guid messageId,
        string? consumerGroup = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InboxStateCount>> GetStateCountsAsync(
        MessageKind? kind = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置为可重新抢占状态（ConsumeFailed）。
    /// </summary>
    Task ResetForRetryAsync(
        Guid messageId,
        string? consumerGroup = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 若存在 Inbox 记录则重置并返回 true；不存在返回 false（不抛异常）。
    /// </summary>
    Task<bool> TryResetForRetryAsync(
        Guid messageId,
        string? consumerGroup = null,
        CancellationToken cancellationToken = default);
}
