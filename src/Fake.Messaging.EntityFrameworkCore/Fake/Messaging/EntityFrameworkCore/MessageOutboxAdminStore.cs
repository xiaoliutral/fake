using Fake.Messaging.Admin;
using Microsoft.EntityFrameworkCore;

namespace Fake.Messaging.EntityFrameworkCore;

public class MessageOutboxAdminStore(MessagingDbContext context) : IMessageOutboxAdminStore
{
    public async Task<MessagingPagedResult<OutboxMessageAdminDto>> QueryAsync(
        MessageOutboxQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageIndex = Math.Max(1, request.PageIndex);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = context.OutboxMessages.AsNoTracking().AsQueryable();

        if (request.Kind.HasValue)
            query = query.Where(e => e.Kind == request.Kind.Value);

        if (request.State.HasValue)
            query = query.Where(e => e.State == request.State.Value);

        if (request.MessageId.HasValue)
            query = query.Where(e => e.MessageId == request.MessageId.Value);

        if (!string.IsNullOrWhiteSpace(request.Destination))
        {
            var dest = request.Destination.Trim();
            query = query.Where(e => e.Destination.Contains(dest));
        }

        if (!string.IsNullOrWhiteSpace(request.TypeName))
        {
            var type = request.TypeName.Trim();
            query = query.Where(e =>
                e.TypeName == type || (e.TypeName != null && e.TypeName.EndsWith("." + type)));
        }

        if (!string.IsNullOrWhiteSpace(request.TransactionId))
        {
            var tx = request.TransactionId.Trim();
            query = query.Where(e => e.TransactionId.Contains(tx));
        }

        if (!string.IsNullOrWhiteSpace(request.Content))
        {
            var content = request.Content.Trim();
            query = query.Where(e => e.Body.Contains(content));
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            if (Guid.TryParse(kw, out var messageId))
            {
                query = query.Where(e =>
                    e.MessageId == messageId ||
                    e.TransactionId.Contains(kw));
            }
            else
            {
                query = query.Where(e =>
                    e.MessageId.ToString().Contains(kw) ||
                    e.TransactionId.Contains(kw));
            }
        }

        if (request.From.HasValue)
        {
            var from = NormalizeUtc(request.From.Value);
            query = query.Where(e => e.CreationTime >= from);
        }

        if (request.To.HasValue)
        {
            var to = NormalizeUtc(request.To.Value);
            query = query.Where(e => e.CreationTime <= to);
        }

        if (request.MinTimesSent.HasValue)
            query = query.Where(e => e.TimesSent >= request.MinTimesSent.Value);

        if (request.MaxTimesSent.HasValue)
            query = query.Where(e => e.TimesSent <= request.MaxTimesSent.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.CreationTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new MessagingPagedResult<OutboxMessageAdminDto>(
            items.Select(ToDto).ToList(),
            total,
            pageIndex,
            pageSize);
    }

    public async Task<OutboxMessageAdminDto?> FindAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entry = await context.OutboxMessages.AsNoTracking()
            .SingleOrDefaultAsync(e => e.MessageId == messageId, cancellationToken);
        return entry == null ? null : ToDto(entry);
    }

    public async Task<IReadOnlyList<OutboxStateCount>> GetStateCountsAsync(
        MessageKind? kind = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.OutboxMessages.AsNoTracking().AsQueryable();
        if (kind.HasValue)
            query = query.Where(e => e.Kind == kind.Value);

        var rows = await query
            .GroupBy(e => e.State)
            .Select(g => new { State = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new OutboxStateCount(r.State, r.Count)).ToList();
    }

    public async Task ResetForRetryAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entry = await context.OutboxMessages
            .SingleOrDefaultAsync(e => e.MessageId == messageId, cancellationToken)
            ?? throw new InvalidOperationException($"Outbox message {messageId} was not found.");

        entry.ResetForRetry();
        await context.SaveChangesAsync(cancellationToken);
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static OutboxMessageAdminDto ToDto(MessageOutboxEntry e)
    {
        var typeName = e.TypeName;
        var shortName = string.IsNullOrEmpty(typeName)
            ? string.Empty
            : typeName.Contains('.') ? typeName.Split('.').Last() : typeName;
        return new(
            e.MessageId,
            e.Destination,
            e.Kind,
            typeName,
            shortName,
            e.State,
            e.TimesSent,
            e.CreationTime,
            e.TransactionId,
            e.Body,
            e.LockExpiresAt,
            e.LastError);
    }
}
