namespace Fake.Messaging;

/// <summary>
/// 未注册 Inbox 存储时的空实现（始终允许消费，不落库）。
/// </summary>
public sealed class NullMessageInbox : IMessageInbox
{
    public static NullMessageInbox Instance { get; } = new();

    public Task<bool> TryBeginAsync(
        Guid messageId,
        string consumerGroup,
        MessageEnvelope envelope,
        bool forceReclaim = false,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task MarkSucceededAsync(Guid messageId, string consumerGroup, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task MarkFailedAsync(Guid messageId, string consumerGroup, string error, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task MarkNoHandlerAsync(Guid messageId, string consumerGroup, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
