namespace Fake.EventBus.Distributed;

/// <summary>
/// 未接入 IntegrationEventLog 时的空实现。
/// </summary>
public sealed class NullOutboxEventPublisher : IOutboxEventPublisher
{
    public static NullOutboxEventPublisher Instance { get; } = new();

    public Task PublishPendingAsync(Guid transactionId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
