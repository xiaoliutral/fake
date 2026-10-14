namespace Fake.EventBus.Distributed;

/// <summary>
/// Inbox 人工重试：在本机重新执行已注册的 EventHandler（不经 RabbitMQ）。
/// </summary>
public interface IInboxEventReprocessor
{
    Task<InboxReprocessResult> ReprocessAsync(Guid eventId, CancellationToken cancellationToken = default);
}
