namespace Fake.EventBus.Distributed;

/// <summary>
/// Inbox 空实现：未接入 IntegrationEventLog 时跳过幂等落库（每次都当作新事件）。
/// </summary>
public sealed class NullInboxEventLogService : IInboxEventLogService
{
    public static NullInboxEventLogService Instance { get; } = new();

    public Task<bool> IsEventProcessedAsync(Guid eventId)
        => Task.FromResult(false);

    public Task SaveProcessedEventAsync(Guid eventId, string eventTypeName, string content)
        => Task.CompletedTask;

    public Task<bool> TryMarkAsProcessingAsync(Guid eventId, string eventTypeName, string content)
        => Task.FromResult(true);

    public Task MarkAsSucceededAsync(Guid eventId)
        => Task.CompletedTask;

    public Task MarkAsFailedAsync(Guid eventId, string errorMessage)
        => Task.CompletedTask;
}
