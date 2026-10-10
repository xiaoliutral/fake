namespace Fake.EventBus.Distributed;

/// <summary>
/// Outbox 空实现：未接入 IntegrationEventLog 时直接 no-op。
/// </summary>
public sealed class NullOutboxEventLogService : IOutboxEventLogService
{
    public static NullOutboxEventLogService Instance { get; } = new();

    public Task<IEnumerable<OutboxEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId)
        => Task.FromResult(Enumerable.Empty<OutboxEventLogEntry>());

    public Task<IEnumerable<OutboxEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<OutboxEventLogEntry>());

    public Task SaveEventAsync(Event integrationEvent, ITransactionContext transactionContext)
        => Task.CompletedTask;

    public Task MarkEventAsPublishedAsync(Guid eventId)
        => Task.CompletedTask;

    public Task<bool> TryMarkEventAsInProgressAsync(Guid eventId, TimeSpan? lockTimeout = null)
        => Task.FromResult(false);

    public Task MarkEventAsFailedAsync(Guid eventId)
        => Task.CompletedTask;
}
