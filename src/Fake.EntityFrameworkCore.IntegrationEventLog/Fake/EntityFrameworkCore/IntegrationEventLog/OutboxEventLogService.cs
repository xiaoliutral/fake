using System.Reflection;
using Fake.EventBus;
using Fake.EventBus.Distributed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

public class OutboxEventLogService(IntegrationEventLogContext integrationEventLogContext)
    : IOutboxEventLogService
{
    private static readonly List<Type> EventTypes =
        Assembly.Load(Assembly.GetEntryAssembly()?.FullName ?? string.Empty)
            .GetTypes()
            .Where(t => t.Name.EndsWith(nameof(IntegrationEvent)))
            .ToList();

    private volatile bool _disposedValue;

    public async Task<IEnumerable<OutboxEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId)
    {
        var tid = transactionId.ToString();

        var result = await integrationEventLogContext.OutboxEventLogs
            .Where(e => e.TransactionId == tid && e.State == EventState.NotPublished).ToListAsync();

        if (result.Any())
        {
            return result.OrderBy(o => o.CreationTime)
                .Select(e =>
                    e.DeserializeJsonContent(EventTypes.Find(t => t.Name == e.EventTypeShortName) ??
                                             throw new FakeException($"非法的事件类型：{e.EventTypeShortName}")));
        }

        return new List<OutboxEventLogEntry>();
    }

    public Task SaveEventAsync(Event @event, ITransactionContext transactionContext)
    {
        var transactionId = transactionContext.TransactionId;
        
        // 根据不同的事务上下文类型处理
        if (transactionContext is EfCoreTransactionContext efTransaction)
        {
            // EF Core 事务：复用底层数据库事务
            var dbTransaction = (IDbContextTransaction)efTransaction.GetUnderlyingTransaction();
            integrationEventLogContext.Database.UseTransaction(dbTransaction.GetDbTransaction());
        }
        // TransactionScope：EF Core 会自动感知环境事务，无需额外操作

        var eventLogEntry = new OutboxEventLogEntry(@event, transactionId);
        integrationEventLogContext.OutboxEventLogs.Add(eventLogEntry);

        return integrationEventLogContext.SaveChangesAsync();
    }

    public Task MarkEventAsPublishedAsync(Guid eventId)
    {
        return UpdateEventStatus(eventId, EventState.Published);
    }

    /// <summary>
    /// 原子性地尝试将事件标记为处理中（分布式锁，支持僵尸锁恢复）
    /// </summary>
    public async Task<bool> TryMarkEventAsInProgressAsync(Guid eventId, TimeSpan? lockTimeout = null)
    {
        var now = DateTime.UtcNow;
        var lockExpiry = lockTimeout.HasValue ? now.Add(lockTimeout.Value) : (DateTime?)null;

        // 使用 ExecuteUpdate 原子性地更新状态
        // 可以抢占的情况：
        // 1. 状态为 NotPublished（未发布）
        // 2. 状态为 InProgress 但锁已过期（僵尸锁恢复）
        var affectedRows = await integrationEventLogContext.OutboxEventLogs
            .Where(e => e.EventId == eventId && 
                       (e.State == EventState.NotPublished || 
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

    private Task UpdateEventStatus(Guid eventId, EventState status)
    {
        var eventLogEntry = integrationEventLogContext.OutboxEventLogs.Single(ie => ie.EventId == eventId);
        eventLogEntry.UpdateEventStatus(status);

        if (status == EventState.InProgress)
            eventLogEntry.TimesSentIncr();

        integrationEventLogContext.OutboxEventLogs.Update(eventLogEntry);

        return integrationEventLogContext.SaveChangesAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                integrationEventLogContext.Dispose();
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