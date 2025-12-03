using Fake.EventBus;
using Fake.EventBus.Distributed;
using Microsoft.EntityFrameworkCore;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

public class InboxEventLogService(IntegrationEventLogContext context) : IInboxEventLogService
{
    private volatile bool _disposedValue;

    public async Task<bool> IsEventProcessedAsync(Guid eventId)
    {
        return await context.InboxEventLogs.AnyAsync(e => e.EventId == eventId);
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
            return false;
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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                context.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
