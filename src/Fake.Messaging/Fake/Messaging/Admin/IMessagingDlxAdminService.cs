using Fake.Messaging;

namespace Fake.Messaging.Admin;

/// <summary>
/// Dashboard 可选死信运维（如 EventBus RabbitMQ DLX）。未注册或 <see cref="IsAvailable"/> 为 false 则不展示。
/// </summary>
public interface IMessagingDlxAdminService
{
    bool IsAvailable { get; }

    Task<IReadOnlyList<MessagingDlxMessageDto>> GetMessagesAsync(
        int maxCount = 50,
        CancellationToken cancellationToken = default);

    Task RequeueAsync(string messageId, CancellationToken cancellationToken = default);

    Task DiscardAsync(string messageId, CancellationToken cancellationToken = default);
}
