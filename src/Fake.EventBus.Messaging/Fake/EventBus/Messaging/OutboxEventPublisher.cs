using Fake.EventBus.Distributed;
using Fake.Messaging;
using Fake.Messaging.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.Messaging;

/// <summary>
/// Commit 后按 transactionId 立即经 Messaging Dispatcher 投递。
/// </summary>
public class OutboxEventPublisher(
    IMessageOutboxStore outboxStore,
    OutboxDispatcher dispatcher,
    EventBusMetrics metrics,
    ILogger<OutboxEventPublisher> logger) : IOutboxEventPublisher
{
    public Task PublishPendingAsync(Guid transactionId, CancellationToken cancellationToken = default)
        => dispatcher.DispatchByTransactionAsync(transactionId.ToString(), cancellationToken);

    public async Task<OutboxPublishOneResult> PublishOneAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var record = await outboxStore.FindAsync(eventId, cancellationToken);
        if (record == null || record.Kind != MessageKind.EventBus)
            throw new InvalidOperationException($"Outbox event {eventId} was not found.");

        var eventType = EventBusMessageFactory.ShortTypeName(record.TypeName);
        try
        {
            var ok = await dispatcher.DispatchOneAsync(eventId, cancellationToken);
            if (!ok)
            {
                return new OutboxPublishOneResult(
                    false,
                    "Event was skipped (already in progress or not in a publishable state).");
            }

            metrics.RecordOutboxDispatched(eventType, "manual");
            return new OutboxPublishOneResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Outbox dispatch failed for event {EventId}", eventId);
            metrics.RecordOutboxDispatchFailed(eventType, "manual");
            return new OutboxPublishOneResult(false, ex.Message);
        }
    }
}
