using System.Data.Common;
using Fake.EventBus;
using Fake.EventBus.Distributed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fake.EventBus.IntegrationEventLog;

/// <summary>
/// EF Core 实现的 Outbox 存储。通过 <see cref="DbTransaction"/> 加入调用方事务（EF / SqlSugar 均可提供）。
/// </summary>
public class OutboxEventLogService(IntegrationEventLogContext integrationEventLogContext)
    : IOutboxEventLogService
{
    public async Task<IEnumerable<OutboxEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId)
    {
        var tid = transactionId.ToString();

        var result = await integrationEventLogContext.OutboxEventLogs
            .Where(e => e.TransactionId == tid && e.State == EventState.NotPublished)
            .OrderBy(o => o.CreationTime)
            .ToListAsync();

        return Deserialize(result);
    }

    public async Task<IEnumerable<OutboxEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maxCount));

        var result = await integrationEventLogContext.OutboxEventLogs
            .Where(e => e.State == EventState.NotPublished || e.State == EventState.PublishFailed)
            .OrderBy(o => o.CreationTime)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

        return Deserialize(result);
    }

    public Task SaveEventAsync(Event @event, ITransactionContext transactionContext)
    {
        EnlistInAmbientTransaction(transactionContext);

        var eventLogEntry = new OutboxEventLogEntry(@event, transactionContext.TransactionId);
        integrationEventLogContext.OutboxEventLogs.Add(eventLogEntry);

        return integrationEventLogContext.SaveChangesAsync();
    }

    public Task MarkEventAsPublishedAsync(Guid eventId)
    {
        return UpdateEventStatus(eventId, EventState.Published);
    }

    public async Task<bool> TryMarkEventAsInProgressAsync(Guid eventId, TimeSpan? lockTimeout = null)
    {
        var now = DateTime.UtcNow;
        var lockExpiry = lockTimeout.HasValue ? now.Add(lockTimeout.Value) : (DateTime?)null;

        var affectedRows = await integrationEventLogContext.OutboxEventLogs
            .Where(e => e.EventId == eventId &&
                        (e.State == EventState.NotPublished ||
                         e.State == EventState.PublishFailed ||
                         (e.State == EventState.InProgress && e.LockExpiresAt < now)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.State, EventState.InProgress)
                .SetProperty(e => e.LockExpiresAt, lockExpiry)
                .SetProperty(e => e.TimesSent, e => e.TimesSent + 1));

        return affectedRows > 0;
    }

    public Task MarkEventAsFailedAsync(Guid eventId)
    {
        return UpdateEventStatus(eventId, EventState.PublishFailed);
    }

    private void EnlistInAmbientTransaction(ITransactionContext transactionContext)
    {
        switch (transactionContext.GetUnderlyingTransaction())
        {
            case DbTransaction dbTransaction:
                integrationEventLogContext.Database.UseTransaction(dbTransaction);
                break;
            case IDbContextTransaction efTransaction:
                integrationEventLogContext.Database.UseTransaction(efTransaction.GetDbTransaction());
                break;
            // TransactionScope / 环境事务：由 EF 自动感知，无需 UseTransaction
        }
    }

    private static IEnumerable<OutboxEventLogEntry> Deserialize(List<OutboxEventLogEntry> entries)
    {
        if (entries.Count == 0)
            return entries;

        return entries.Select(e =>
            e.DeserializeJsonContent(IntegrationEventTypeResolver.Resolve(e.EventTypeName)));
    }

    private Task UpdateEventStatus(Guid eventId, EventState status)
    {
        var eventLogEntry = integrationEventLogContext.OutboxEventLogs.Single(ie => ie.EventId == eventId);
        eventLogEntry.UpdateEventStatus(status);

        if (status == EventState.InProgress)
            eventLogEntry.TimesSentIncr();

        integrationEventLogContext.OutboxEventLogs.Update(eventLogEntry);

        return integrationEventLogContext.SaveChangesAsync();
    }
}
