using Fake.EventBus.Distributed;
using Microsoft.EntityFrameworkCore;

namespace Fake.EventBus.IntegrationEventLog;

public class InboxEventLogService(IntegrationEventLogContext context) : IInboxEventLogService
{
    /// <summary>
    /// Consuming 超过此时长视为僵尸，允许重新抢占
    /// </summary>
    private static readonly TimeSpan ProcessingLockTimeout = TimeSpan.FromMinutes(5);

    public async Task<bool> IsEventProcessedAsync(Guid eventId)
    {
        return await context.InboxEventLogs.AnyAsync(e =>
            e.EventId == eventId && e.State == EventState.ConsumeSucceeded);
    }

    public async Task SaveProcessedEventAsync(Guid eventId, string eventTypeName, string content)
    {
        var entry = new InboxEventLogEntry(eventId, eventTypeName, content);
        context.InboxEventLogs.Add(entry);
        await context.SaveChangesAsync();
    }

    public async Task<bool> TryMarkAsProcessingAsync(Guid eventId, string eventTypeName, string content)
    {
        try
        {
            var entry = new InboxEventLogEntry(eventId, eventTypeName, content);
            context.InboxEventLogs.Add(entry);
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            context.ChangeTracker.Clear();
            return await TryReclaimAsync(eventId);
        }
    }

    public async Task MarkAsSucceededAsync(Guid eventId)
    {
        var entry = await context.InboxEventLogs.FindAsync(eventId);
        if (entry != null)
        {
            entry.MarkAsSucceeded();
            await context.SaveChangesAsync();
        }
    }

    public async Task MarkAsFailedAsync(Guid eventId, string errorMessage)
    {
        var entry = await context.InboxEventLogs.FindAsync(eventId);
        if (entry != null)
        {
            entry.MarkAsFailed(errorMessage);
            await context.SaveChangesAsync();
        }
    }

    private async Task<bool> TryReclaimAsync(Guid eventId)
    {
        var existing = await context.InboxEventLogs.FindAsync(eventId);
        if (existing == null)
            return false;

        if (existing.State == EventState.ConsumeSucceeded)
            return false;

        var now = DateTime.UtcNow;
        var canReclaim = existing.State == EventState.ConsumeFailed ||
                         (existing.State == EventState.Consuming &&
                          existing.ProcessedTime < now - ProcessingLockTimeout);

        if (!canReclaim)
            return false;

        existing.MarkAsConsuming();
        await context.SaveChangesAsync();
        return true;
    }

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
