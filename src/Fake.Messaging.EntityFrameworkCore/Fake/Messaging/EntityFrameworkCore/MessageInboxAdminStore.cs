using Fake.Messaging.Admin;
using Microsoft.EntityFrameworkCore;

namespace Fake.Messaging.EntityFrameworkCore;

public class MessageInboxAdminStore(MessagingDbContext context) : IMessageInboxAdminStore
{
    public async Task<MessagingPagedResult<InboxMessageAdminDto>> QueryAsync(
        MessageInboxQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageIndex = Math.Max(1, request.PageIndex);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = context.InboxMessages.AsNoTracking().AsQueryable();

        if (request.Kind.HasValue)
            query = query.Where(e => e.Kind == request.Kind.Value);

        if (request.State.HasValue)
            query = query.Where(e => e.State == request.State.Value);

        if (request.MessageId.HasValue)
            query = query.Where(e => e.MessageId == request.MessageId.Value);

        if (!string.IsNullOrWhiteSpace(request.ConsumerGroup))
        {
            var group = request.ConsumerGroup.Trim();
            query = query.Where(e => e.ConsumerGroup == group);
        }

        if (!string.IsNullOrWhiteSpace(request.Destination))
        {
            var dest = request.Destination.Trim();
            query = query.Where(e => e.Destination != null && e.Destination.Contains(dest));
        }

        if (!string.IsNullOrWhiteSpace(request.TypeName))
        {
            var type = request.TypeName.Trim();
            query = query.Where(e =>
                e.TypeName == type || (e.TypeName != null && e.TypeName.EndsWith("." + type)));
        }

        if (!string.IsNullOrWhiteSpace(request.Content))
        {
            var content = request.Content.Trim();
            query = query.Where(e => e.Body.Contains(content));
        }

        if (!string.IsNullOrWhiteSpace(request.Error))
        {
            var error = request.Error.Trim();
            query = query.Where(e => e.ErrorMessage != null && e.ErrorMessage.Contains(error));
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            if (Guid.TryParse(kw, out var messageId))
            {
                query = query.Where(e =>
                    e.MessageId == messageId ||
                    (e.ErrorMessage != null && e.ErrorMessage.Contains(kw)));
            }
            else
            {
                query = query.Where(e =>
                    e.MessageId.ToString().Contains(kw) ||
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

        return new MessagingPagedResult<InboxMessageAdminDto>(
            items.Select(ToDto).ToList(),
            total,
            pageIndex,
            pageSize);
    }

    public async Task<InboxMessageAdminDto?> FindAsync(
        Guid messageId,
        string? consumerGroup = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.InboxMessages.AsNoTracking()
            .Where(e => e.MessageId == messageId);

        if (!string.IsNullOrWhiteSpace(consumerGroup))
            query = query.Where(e => e.ConsumerGroup == consumerGroup.Trim());

        var entry = await query
            .OrderByDescending(e => e.ProcessedTime)
            .FirstOrDefaultAsync(cancellationToken);
        return entry == null ? null : ToDto(entry);
    }

    public async Task<IReadOnlyList<InboxStateCount>> GetStateCountsAsync(
        MessageKind? kind = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.InboxMessages.AsNoTracking().AsQueryable();
        if (kind.HasValue)
            query = query.Where(e => e.Kind == kind.Value);

        var rows = await query
            .GroupBy(e => e.State)
            .Select(g => new { State = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new InboxStateCount(r.State, r.Count)).ToList();
    }

    public async Task ResetForRetryAsync(
        Guid messageId,
        string? consumerGroup = null,
        CancellationToken cancellationToken = default)
    {
        if (!await TryResetForRetryAsync(messageId, consumerGroup, cancellationToken))
            throw new InvalidOperationException($"Inbox message {messageId} was not found.");
    }

    public async Task<bool> TryResetForRetryAsync(
        Guid messageId,
        string? consumerGroup = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.InboxMessages.Where(e => e.MessageId == messageId);
        if (!string.IsNullOrWhiteSpace(consumerGroup))
            query = query.Where(e => e.ConsumerGroup == consumerGroup.Trim());

        var entry = await query
            .OrderByDescending(e => e.ProcessedTime)
            .FirstOrDefaultAsync(cancellationToken);
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

    private static InboxMessageAdminDto ToDto(MessageInboxEntry e)
    {
        var typeName = e.TypeName;
        var shortName = string.IsNullOrEmpty(typeName)
            ? string.Empty
            : typeName.Contains('.') ? typeName.Split('.').Last() : typeName;
        return new(
            e.MessageId,
            e.ConsumerGroup,
            e.Kind,
            e.Destination,
            typeName,
            shortName,
            e.State,
            e.ProcessedTime,
            e.ErrorMessage,
            e.Body);
    }
}
