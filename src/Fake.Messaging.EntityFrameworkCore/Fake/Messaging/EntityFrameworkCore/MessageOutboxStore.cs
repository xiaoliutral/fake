using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fake.Messaging.EntityFrameworkCore;

public class MessageOutboxStore(MessagingDbContext db) : IMessageOutboxStore
{
    public async Task EnqueueAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        db.OutboxMessages.Add(new MessageOutboxEntry(envelope));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 征用调用方事务后写入 Outbox（EF / ADO 事务均可）。
    /// </summary>
    public async Task EnqueueInTransactionAsync(
        MessageEnvelope envelope,
        object underlyingTransaction,
        CancellationToken cancellationToken = default)
    {
        EnlistInAmbientTransaction(underlyingTransaction);
        db.OutboxMessages.Add(new MessageOutboxEntry(envelope));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entry = await db.OutboxMessages.SingleAsync(e => e.MessageId == messageId, cancellationToken);
        entry.UpdateState(MessageOutboxState.Published);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkPublishFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var entry = await db.OutboxMessages.SingleAsync(e => e.MessageId == messageId, cancellationToken);
        entry.UpdateState(MessageOutboxState.PublishFailed, error);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryMarkInProgressAsync(
        Guid messageId,
        TimeSpan? lockTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var lockExpiry = lockTimeout.HasValue ? now.Add(lockTimeout.Value) : (DateTime?)null;

        var affected = await db.OutboxMessages
            .Where(e => e.MessageId == messageId &&
                        (e.State == MessageOutboxState.NotPublished ||
                         e.State == MessageOutboxState.PublishFailed ||
                         (e.State == MessageOutboxState.InProgress && e.LockExpiresAt < now)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.State, MessageOutboxState.InProgress)
                .SetProperty(e => e.LockExpiresAt, lockExpiry)
                .SetProperty(e => e.TimesSent, e => e.TimesSent + 1), cancellationToken);

        return affected > 0;
    }

    public async Task<IReadOnlyList<MessageOutboxRecord>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maxCount));

        var rows = await db.OutboxMessages
            .AsNoTracking()
            .Where(e => e.State == MessageOutboxState.NotPublished ||
                        e.State == MessageOutboxState.PublishFailed)
            .OrderBy(e => e.CreationTime)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

        return rows.Select(e => e.ToRecord()).ToList();
    }

    public async Task<IReadOnlyList<MessageOutboxRecord>> GetPendingByTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.OutboxMessages
            .AsNoTracking()
            .Where(e => e.TransactionId == transactionId && e.State == MessageOutboxState.NotPublished)
            .OrderBy(e => e.CreationTime)
            .ToListAsync(cancellationToken);

        return rows.Select(e => e.ToRecord()).ToList();
    }

    public async Task<MessageOutboxRecord?> FindAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entry = await db.OutboxMessages.AsNoTracking()
            .SingleOrDefaultAsync(e => e.MessageId == messageId, cancellationToken);
        return entry?.ToRecord();
    }

    public async Task ResetForRetryAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entry = await db.OutboxMessages
            .SingleOrDefaultAsync(e => e.MessageId == messageId, cancellationToken)
            ?? throw new InvalidOperationException($"Outbox message {messageId} was not found.");

        entry.ResetForRetry();
        await db.SaveChangesAsync(cancellationToken);
    }

    private void EnlistInAmbientTransaction(object underlyingTransaction)
    {
        var dbTransaction = underlyingTransaction switch
        {
            DbTransaction ado => ado,
            IDbContextTransaction efTransaction => efTransaction.GetDbTransaction(),
            _ => null
        };

        if (dbTransaction == null)
            return;

        var ownerConnection = dbTransaction.Connection
                              ?? throw new InvalidOperationException(
                                  "Ambient DbTransaction has no connection; cannot enlist Messaging Outbox.");

        var database = db.Database;
        var current = database.GetDbConnection();
        if (!ReferenceEquals(current, ownerConnection))
        {
            if (current.State == ConnectionState.Open)
                current.Close();

            database.SetDbConnection(ownerConnection, contextOwnsConnection: false);
        }

        if (database.CurrentTransaction?.GetDbTransaction() != dbTransaction)
            database.UseTransaction(dbTransaction);
    }
}
