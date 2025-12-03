using Fake.EventBus;
using Fake.EventBus.Distributed;
using Microsoft.EntityFrameworkCore;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

/// <summary>
/// Outbox 事件日志服务扩展方法（让开发无感）
/// </summary>
public static class OutboxEventLogServiceExtensions
{
    /// <summary>
    /// 保存事件到 Outbox（自动从 DbContext 获取事务）
    /// </summary>
    /// <param name="service">Outbox 服务</param>
    /// <param name="event">集成事件</param>
    /// <param name="dbContext">业务 DbContext（自动获取当前事务）</param>
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

        var transactionContext = new EfCoreTransactionContext(currentTransaction);
        await service.SaveEventAsync(@event, transactionContext);
    }

    /// <summary>
    /// 保存事件到 Outbox（自动检测 TransactionScope 环境事务）
    /// </summary>
    /// <param name="service">Outbox 服务</param>
    /// <param name="event">集成事件</param>
    public static async Task SaveEventAsync(
        this IOutboxEventLogService service,
        Event @event)
    {
        var ambientTransaction = System.Transactions.Transaction.Current;
        if (ambientTransaction == null)
        {
            throw new InvalidOperationException(
                "没有检测到环境事务。请在 TransactionScope 内调用此方法，或使用 DbContext 重载。");
        }

        using var scope = new System.Transactions.TransactionScope(
            System.Transactions.TransactionScopeOption.Required,
            System.Transactions.TransactionScopeAsyncFlowOption.Enabled);

        var transactionContext = new TransactionScopeContext(scope);
        await service.SaveEventAsync(@event, transactionContext);
        
        scope.Complete();
    }
}
