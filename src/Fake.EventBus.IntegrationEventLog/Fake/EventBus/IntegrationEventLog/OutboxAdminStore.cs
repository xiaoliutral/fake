using Fake.EventBus.Distributed;
using Microsoft.EntityFrameworkCore;

namespace Fake.EventBus.IntegrationEventLog;

public class OutboxAdminStore(IntegrationEventLogContext context) : IOutboxAdminStore
{
    public async Task<EventBusPagedResult<OutboxMessageDto>> QueryAsync(
        OutboxQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageIndex = Math.Max(1, request.PageIndex);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = context.OutboxEventLogs.AsNoTracking().AsQueryable();

        if (request.State.HasValue)
            query = query.Where(e => e.State == request.State.Value);

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            var type = request.EventType.Trim();
            query = query.Where(e =>
                e.EventTypeName == type || e.EventTypeName.EndsWith("." + type));
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            query = query.Where(e =>
                e.EventId.ToString().Contains(kw) ||
                e.TransactionId.Contains(kw) ||
                e.Content.Contains(kw));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.CreationTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new EventBusPagedResult<OutboxMessageDto>(
            items.Select(ToDto).ToList(),
            total,
            pageIndex,
            pageSize);
    }

    public async Task<OutboxMessageDto?> FindAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entry = await context.OutboxEventLogs.AsNoTracking()
            .SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        return entry == null ? null : ToDto(entry);
    }

    public async Task<IReadOnlyList<EventStateCount>> GetStateCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await context.OutboxEventLogs.AsNoTracking()
            .GroupBy(e => e.State)
            .Select(g => new { State = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new EventStateCount(r.State, r.Count)).ToList();
    }

    public async Task ResetForRetryAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entry = await context.OutboxEventLogs
            .SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken)
            ?? throw new InvalidOperationException($"Outbox event {eventId} was not found.");

        entry.ResetForRetry();
        await context.SaveChangesAsync(cancellationToken);
    }

    private static OutboxMessageDto ToDto(OutboxEventLogEntry e) => new(
        e.EventId,
        e.EventTypeName,
        e.EventTypeShortName,
        e.State,
        e.TimesSent,
        e.CreationTime,
        e.TransactionId,
        e.Content,
        e.LockExpiresAt);
}
