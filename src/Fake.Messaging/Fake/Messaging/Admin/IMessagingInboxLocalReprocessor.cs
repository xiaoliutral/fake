using Fake.Messaging;

namespace Fake.Messaging.Admin;

/// <summary>
/// Dashboard 可选 Inbox 本地重跑（如 EventBus Handler Reprocess）。未注册则仅支持重置状态。
/// </summary>
public interface IMessagingInboxLocalReprocessor
{
    Task<MessageInboxReprocessResult> ReprocessAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);
}
