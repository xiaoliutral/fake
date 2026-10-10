using Fake.EventBus;
using Fake.EventBus.Distributed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fake.EventBus.IntegrationEventLog;

/// <summary>
/// EF 侧显式 Outbox 扩展（业务无感路径请用 IDistributedEventBus）。
/// </summary>
public static class OutboxEventLogServiceExtensions
{
    /// <summary>
    /// 保存事件到 Outbox（加入指定 DbContext 的当前事务）
    /// </summary>
    public static async Task SaveEventAsync(
        this IOutboxEventLogService service,
        Event @event,
        DbContext dbContext)
    {
        var currentTransaction = dbContext.Database.CurrentTransaction;
        if (currentTransaction == null)
        {
            throw new InvalidOperationException(
                "当前 DbContext 没有活动事务。请在 BeginTransaction 内调用此方法，或使用 TransactionScope。");
        }

        await service.SaveEventAsync(
            @event,
            new DbTransactionContext(currentTransaction.TransactionId, currentTransaction.GetDbTransaction()));
    }

    /// <summary>
    /// 保存事件到 Outbox（使用当前环境 TransactionScope，不创建嵌套 Scope）
    /// </summary>
    public static async Task SaveEventAsync(
        this IOutboxEventLogService service,
        Event @event)
    {
        if (System.Transactions.Transaction.Current == null)
        {
            throw new InvalidOperationException(
                "没有检测到环境事务。请在 TransactionScope 内调用此方法，或使用 DbContext 重载。");
        }

        await service.SaveEventAsync(@event, new AmbientTransactionContext());
    }
}
