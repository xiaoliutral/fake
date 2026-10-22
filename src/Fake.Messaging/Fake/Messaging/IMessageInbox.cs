namespace Fake.Messaging;

public interface IMessageInbox
{
    /// <summary>
    /// 尝试开始消费。返回 false 表示已被其它消费者占用或已终态。
    /// <paramref name="forceReclaim"/> 为 true 时（如 Redelivered）可强制抢占锁。
    /// </summary>
    Task<bool> TryBeginAsync(
        Guid messageId,
        string consumerGroup,
        MessageEnvelope envelope,
        bool forceReclaim = false,
        CancellationToken cancellationToken = default);

    Task MarkSucceededAsync(Guid messageId, string consumerGroup, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid messageId, string consumerGroup, string error, CancellationToken cancellationToken = default);

    Task MarkNoHandlerAsync(Guid messageId, string consumerGroup, CancellationToken cancellationToken = default);
}
