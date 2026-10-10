using Fake.EventBus.Distributed;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.IntegrationEventLog;

/// <summary>
/// eShop 风格：Commit 后按 transactionId 立即发布；失败留给后台扫描重试。
/// </summary>
public class OutboxEventPublisher(
    IOutboxEventLogService eventLogService,
    IPhysicalDistributedEventBus eventBus,
    ILogger<OutboxEventPublisher> logger) : IOutboxEventPublisher
{
    public async Task PublishPendingAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var pendingEvents = (await eventLogService.RetrieveEventLogsPendingToPublishAsync(transactionId))
            .ToList();

        foreach (var eventLog in pendingEvents)
        {
            try
            {
                var lockAcquired = await eventLogService.TryMarkEventAsInProgressAsync(eventLog.EventId);
                if (!lockAcquired)
                {
                    logger.LogDebug("Event {EventId} skipped (already in progress or published)", eventLog.EventId);
                    continue;
                }

                if (eventLog.IntegrationEvent is not IntegrationEvent integrationEvent)
                {
                    throw new InvalidOperationException(
                        $"Outbox entry {eventLog.EventId} has no deserializable IntegrationEvent.");
                }

                await eventBus.PublishAsync(integrationEvent, cancellationToken);
                await eventLogService.MarkEventAsPublishedAsync(eventLog.EventId);

                logger.LogInformation(
                    "Outbox event {EventId} dispatched immediately after commit (transaction {TransactionId})",
                    eventLog.EventId, transactionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Outbox immediate dispatch failed for event {EventId}; background scanner will retry",
                    eventLog.EventId);
                await eventLogService.MarkEventAsFailedAsync(eventLog.EventId);
            }
        }
    }
}
