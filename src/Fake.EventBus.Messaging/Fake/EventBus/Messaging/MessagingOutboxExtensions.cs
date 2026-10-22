using Fake.EventBus;
using Fake.EventBus.Distributed;
using Fake.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.EventBus.Messaging;

/// <summary>
/// 显式将 IntegrationEvent 写入 Messaging Outbox（业务无感路径请用 <see cref="IDistributedEventBus"/>）。
/// </summary>
public static class MessagingOutboxExtensions
{
    public static async Task EnqueueIntegrationEventAsync(
        this IMessageOutboxStore store,
        Event @event,
        DbContext dbContext,
        IEventRoutingKeyResolver routingKeyResolver,
        CancellationToken cancellationToken = default)
    {
        var currentTransaction = dbContext.Database.CurrentTransaction
                                 ?? throw new InvalidOperationException(
                                     "当前 DbContext 没有活动事务。请在 BeginTransaction 内调用此方法，或使用 TransactionScope。");

        var envelope = EventBusMessageFactory.Create(
            @event, routingKeyResolver, currentTransaction.TransactionId.ToString());
        await store.EnqueueInTransactionAsync(
            envelope, currentTransaction.GetDbTransaction(), cancellationToken);
    }

    public static async Task EnqueueIntegrationEventAsync(
        this IMessageOutboxStore store,
        Event @event,
        IEventRoutingKeyResolver routingKeyResolver,
        CancellationToken cancellationToken = default)
    {
        if (System.Transactions.Transaction.Current == null)
        {
            throw new InvalidOperationException(
                "没有检测到环境事务。请在 TransactionScope 内调用此方法，或使用 DbContext 重载。");
        }

        var envelope = EventBusMessageFactory.Create(
            @event, routingKeyResolver, AmbientTransactionId.Current.ToString());
        await store.EnqueueInTransactionAsync(
            envelope, System.Transactions.Transaction.Current, cancellationToken);
    }

    /// <summary>
    /// 从 DI 解析 routingKeyResolver 的便捷重载。
    /// </summary>
    public static Task EnqueueIntegrationEventAsync(
        this IMessageOutboxStore store,
        Event @event,
        DbContext dbContext,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
        => store.EnqueueIntegrationEventAsync(
            @event, dbContext, services.GetRequiredService<IEventRoutingKeyResolver>(), cancellationToken);
}
