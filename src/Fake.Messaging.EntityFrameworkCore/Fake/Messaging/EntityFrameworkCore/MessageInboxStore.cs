using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fake.Messaging.EntityFrameworkCore;

public class MessageInboxStore(
    MessagingDbContext db,
    IOptions<MessagingOptions> options) : IMessageInboxStore
{
    private readonly MessagingOptions _options = options.Value;

    public async Task<bool> TryBeginAsync(
        Guid messageId,
        string consumerGroup,
        MessageEnvelope envelope,
        bool forceReclaim = false,
        CancellationToken cancellationToken = default)
    {
        // 同一 DbContext 内已跟踪时直接走抢占，避免 Add 主键冲突
        var tracked = await FindTrackedAsync(messageId, consumerGroup, cancellationToken);
        if (tracked != null)
            return await TryReclaimAsync(messageId, consumerGroup, forceReclaim, cancellationToken);

        try
        {
            db.InboxMessages.Add(new MessageInboxEntry(messageId, consumerGroup, envelope));
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            db.ChangeTracker.Clear();
            return await TryReclaimAsync(messageId, consumerGroup, forceReclaim, cancellationToken);
        }
    }

    public async Task MarkSucceededAsync(
        Guid messageId,
        string consumerGroup,
        CancellationToken cancellationToken = default)
    {
        var entry = await FindTrackedAsync(messageId, consumerGroup, cancellationToken);
        if (entry == null)
            return;

        entry.MarkAsSucceeded();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string consumerGroup,
        string error,
        CancellationToken cancellationToken = default)
    {
        var entry = await FindTrackedAsync(messageId, consumerGroup, cancellationToken);
        if (entry == null)
            return;

        entry.MarkAsFailed(error);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkNoHandlerAsync(
        Guid messageId,
        string consumerGroup,
        CancellationToken cancellationToken = default)
    {
        var entry = await FindTrackedAsync(messageId, consumerGroup, cancellationToken);
        if (entry == null)
            return;

        entry.MarkAsNoHandler();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetForRetryAsync(
        Guid messageId,
        string consumerGroup,
        CancellationToken cancellationToken = default)
    {
        var entry = await FindTrackedAsync(messageId, consumerGroup, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Inbox message {messageId} / {consumerGroup} was not found.");

        entry.ResetForRetry();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MessageInboxRecord?> FindAsync(
        Guid messageId,
        string consumerGroup,
        CancellationToken cancellationToken = default)
    {
        var entry = await db.InboxMessages.AsNoTracking()
            .SingleOrDefaultAsync(
                e => e.MessageId == messageId && e.ConsumerGroup == consumerGroup,
                cancellationToken);
        return entry?.ToRecord();
    }

    private async Task<bool> TryReclaimAsync(
        Guid messageId,
        string consumerGroup,
        bool forceReclaim,
        CancellationToken cancellationToken)
    {
        var existing = await FindTrackedAsync(messageId, consumerGroup, cancellationToken);
        if (existing == null)
            return false;

        if (existing.State == MessageInboxState.ConsumeSucceeded)
            return false;

        var lockTimeout = _options.ResolveProcessingLockTimeout(existing.TypeName);
        var now = DateTime.UtcNow;
        var consumingExpired = existing.State == MessageInboxState.Consuming &&
                               existing.ProcessedTime < now - lockTimeout;

        var canReclaim = existing.State == MessageInboxState.ConsumeFailed ||
                         existing.State == MessageInboxState.NoHandler ||
                         consumingExpired ||
                         (forceReclaim && existing.State == MessageInboxState.Consuming);

        if (!canReclaim)
            return false;

        existing.MarkAsConsuming();
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<MessageInboxEntry?> FindTrackedAsync(
        Guid messageId,
        string consumerGroup,
        CancellationToken cancellationToken)
        => db.InboxMessages.SingleOrDefaultAsync(
            e => e.MessageId == messageId && e.ConsumerGroup == consumerGroup,
            cancellationToken);

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("2627") ||
               message.Contains("2601") ||
               message.Contains("23505") ||
               message.Contains("1062") ||
               message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
