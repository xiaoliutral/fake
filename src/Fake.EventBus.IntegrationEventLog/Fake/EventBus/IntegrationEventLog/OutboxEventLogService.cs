using System.Data;
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

    public async Task<OutboxEventLogEntry?> FindAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entry = await integrationEventLogContext.OutboxEventLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);

        if (entry == null)
            return null;

        return entry.DeserializeJsonContent(IntegrationEventTypeResolver.Resolve(entry.EventTypeName));
    }

    public async Task ResetEventForRetryAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entry = await integrationEventLogContext.OutboxEventLogs
            .SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken)
            ?? throw new InvalidOperationException($"Outbox event {eventId} was not found.");

        entry.ResetForRetry();
        await integrationEventLogContext.SaveChangesAsync(cancellationToken);
    }

    private void EnlistInAmbientTransaction(ITransactionContext transactionContext)
    {
        var dbTransaction = transactionContext.GetUnderlyingTransaction() switch
        {
            DbTransaction ado => ado,
            IDbContextTransaction efTransaction => efTransaction.GetDbTransaction(),
            _ => null
        };

        // 环境事务（TransactionScope）由 EF 自动感知。
        if (dbTransaction == null)
        {
            return;
        }

        var ownerConnection = dbTransaction.Connection
                              ?? throw new InvalidOperationException(
                                  "Ambient DbTransaction has no connection; cannot enlist Outbox.");

        var database = integrationEventLogContext.Database;
        var current = database.GetDbConnection();
        // SqlSugar 与 EF 各开连接时，必须先把 Outbox 上下文切到事务所属连接，否则 UseTransaction 会抛
        // "The specified transaction is not associated with the current connection"。
        if (!ReferenceEquals(current, ownerConnection))
        {
            if (current.State == ConnectionState.Open)
            {
                current.Close();
            }

            database.SetDbConnection(ownerConnection, contextOwnsConnection: false);
        }

        if (database.CurrentTransaction?.GetDbTransaction() != dbTransaction)
        {
            database.UseTransaction(dbTransaction);
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
