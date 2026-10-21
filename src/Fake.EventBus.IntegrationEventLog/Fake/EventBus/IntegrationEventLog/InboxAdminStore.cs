using Fake.EventBus.Distributed;
using Microsoft.EntityFrameworkCore;

namespace Fake.EventBus.IntegrationEventLog;

public class InboxAdminStore(IntegrationEventLogContext context) : IInboxAdminStore
{
    public async Task<EventBusPagedResult<InboxMessageDto>> QueryAsync(
        InboxQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageIndex = Math.Max(1, request.PageIndex);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = context.InboxEventLogs.AsNoTracking().AsQueryable();

        if (request.State.HasValue)
            query = query.Where(e => e.State == request.State.Value);

        if (request.EventId.HasValue)
            query = query.Where(e => e.EventId == request.EventId.Value);

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            var type = request.EventType.Trim();
            query = query.Where(e =>
                e.EventTypeName == type || e.EventTypeName.EndsWith("." + type));
        }

        if (!string.IsNullOrWhiteSpace(request.Content))
        {
            var content = request.Content.Trim();
            query = query.Where(e => e.Content.Contains(content));
        }

        if (!string.IsNullOrWhiteSpace(request.Error))
        {
            var error = request.Error.Trim();
            query = query.Where(e => e.ErrorMessage != null && e.ErrorMessage.Contains(error));
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            if (Guid.TryParse(kw, out var eventId))
            {
                query = query.Where(e =>
                    e.EventId == eventId ||
                    (e.ErrorMessage != null && e.ErrorMessage.Contains(kw)));
            }
            else
            {
                query = query.Where(e =>
                    e.EventId.ToString().Contains(kw) ||
                    (e.ErrorMessage != null && e.ErrorMessage.Contains(kw)));
            }
        }

        if (request.From.HasValue)
        {
            var from = NormalizeUtc(request.From.Value);
            query = query.Where(e => e.ProcessedTime >= from);
        }

        if (request.To.HasValue)
        {
            var to = NormalizeUtc(request.To.Value);
            query = query.Where(e => e.ProcessedTime <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.ProcessedTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new EventBusPagedResult<InboxMessageDto>(
            items.Select(ToDto).ToList(),
            total,
            pageIndex,
            pageSize);
    }

    public async Task<InboxMessageDto?> FindAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entry = await context.InboxEventLogs.AsNoTracking()
            .SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        return entry == null ? null : ToDto(entry);
    }

    public async Task<IReadOnlyList<EventStateCount>> GetStateCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await context.InboxEventLogs.AsNoTracking()
            .GroupBy(e => e.State)
            .Select(g => new { State = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new EventStateCount(r.State, r.Count)).ToList();
    }

    public async Task ResetForRetryAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (!await TryResetForRetryAsync(eventId, cancellationToken))
            throw new InvalidOperationException($"Inbox event {eventId} was not found.");
    }

    public async Task<bool> TryResetForRetryAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entry = await context.InboxEventLogs
            .SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (entry == null)
            return false;

        entry.ResetForRetry();
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static InboxMessageDto ToDto(InboxEventLogEntry e) => new(
        e.EventId,
        e.EventTypeName,
        e.EventTypeShortName,
        e.State,
        e.ProcessedTime,
        e.ErrorMessage,
        e.Content);
}
