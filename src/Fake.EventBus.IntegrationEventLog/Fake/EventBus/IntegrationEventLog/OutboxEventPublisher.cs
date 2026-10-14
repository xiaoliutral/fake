using Fake.EventBus.Distributed;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.IntegrationEventLog;

/// <summary>
/// eShop 风格：Commit 后按 transactionId 立即发布；失败留给后台扫描重试。
/// </summary>
public class OutboxEventPublisher(
    IOutboxEventLogService eventLogService,
    IPhysicalDistributedEventBus eventBus,
    EventBusMetrics metrics,
    ILogger<OutboxEventPublisher> logger) : IOutboxEventPublisher
{
    public async Task PublishPendingAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var pendingEvents = (await eventLogService.RetrieveEventLogsPendingToPublishAsync(transactionId))
            .ToList();

        foreach (var eventLog in pendingEvents)
        {
            await DispatchAsync(eventLog, source: "immediate", throwOnFailure: false, cancellationToken);
        }
    }

    public async Task<OutboxPublishOneResult> PublishOneAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var eventLog = await eventLogService.FindAsync(eventId, cancellationToken)
                       ?? throw new InvalidOperationException($"Outbox event {eventId} was not found.");

        // Dashboard 人工重试：失败返回结果，不抛异常（避免 API 500）
        return await DispatchAsync(eventLog, source: "manual", throwOnFailure: false, cancellationToken);
    }

    private async Task<OutboxPublishOneResult> DispatchAsync(
        OutboxEventLogEntry eventLog,
        string source,
        bool throwOnFailure,
        CancellationToken cancellationToken)
    {
        var eventType = eventLog.EventTypeShortName;
        try
        {
            var lockAcquired = await eventLogService.TryMarkEventAsInProgressAsync(eventLog.EventId);
            if (!lockAcquired)
            {
                logger.LogDebug("Event {EventId} skipped (already in progress or published)", eventLog.EventId);
                var current = await eventLogService.FindAsync(eventLog.EventId, cancellationToken);
                return new OutboxPublishOneResult(
                    false,
                    "Event was skipped (already in progress or not in a publishable state).",
                    current?.State);
            }

            if (eventLog.IntegrationEvent is not IntegrationEvent integrationEvent)
            {
                throw new InvalidOperationException(
                    $"Outbox entry {eventLog.EventId} has no deserializable IntegrationEvent.");
            }

            await eventBus.PublishAsync(integrationEvent, cancellationToken);
            await eventLogService.MarkEventAsPublishedAsync(eventLog.EventId);
            metrics.RecordOutboxDispatched(eventType, source);

            logger.LogInformation(
                "Outbox event {EventId} dispatched (source={Source}, transaction {TransactionId})",
                eventLog.EventId, source, eventLog.TransactionId);

            return new OutboxPublishOneResult(true, State: EventState.Published);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Outbox dispatch failed for event {EventId} (source={Source})",
                eventLog.EventId, source);
            metrics.RecordOutboxDispatchFailed(eventType, source);
            await eventLogService.MarkEventAsFailedAsync(eventLog.EventId);

            if (throwOnFailure)
                throw;

            return new OutboxPublishOneResult(false, ex.Message, EventState.PublishFailed);
        }
    }
}
