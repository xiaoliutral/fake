using Fake.EventBus.Distributed;
using Fake.Messaging;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.Messaging;

/// <summary>
/// 默认 Outbox 总线：事务性 UoW 内写 Messaging Outbox；Commit 后立刻投递；后台扫描兜底。
/// </summary>
public class OutboxDistributedEventBus(
    IUnitOfWorkManager unitOfWorkManager,
    IPhysicalDistributedEventBus physicalDistributedEventBus,
    IEventRoutingKeyResolver routingKeyResolver,
    EventBusMetrics metrics,
    ILogger<OutboxDistributedEventBus> logger) : IDistributedEventBus
{
    private const string PendingEventsKey = "Fake.EventBus.Outbox.PendingEvents";
    private const string TransactionIdsKey = "Fake.EventBus.Outbox.TransactionIds";
    private const string HandlersRegisteredKey = "Fake.EventBus.Outbox.HandlersRegistered";

    public Task PublishAsync(Event @event, CancellationToken cancellationToken = default)
        => PublishAsync((IntegrationEvent)@event, cancellationToken);

    public async Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var uow = unitOfWorkManager.Current;
        if (uow == null || !uow.Context.IsTransactional)
        {
            logger.LogWarning(
                "No transactional UoW for event {EventId} ({EventType}); publishing immediately via physical bus (no outbox atomicity)",
                @event.Id, @event.GetType().Name);
            await physicalDistributedEventBus.PublishAsync(@event, cancellationToken);
            metrics.RecordOutboxDispatched(@event.GetType().Name, "immediate-no-tx");
            return;
        }

        var enlistable = FindEnlistableTransaction(uow);
        if (enlistable != null && enlistable.GetDbTransaction() != null)
        {
            await SaveToOutboxAsync(uow, @event, enlistable);
        }
        else
        {
            var pending = uow.GetOrAddItem(PendingEventsKey, _ => new List<IntegrationEvent>());
            pending.Add(@event);
            logger.LogInformation(
                "Outbox event {EventId} ({EventType}) queued until an enlistable DB transaction is available",
                @event.Id, @event.GetType().Name);
        }

        EnsureHandlersRegistered(uow);
    }

    private void EnsureHandlersRegistered(IUnitOfWork uow)
    {
        if (uow.Items.ContainsKey(HandlersRegisteredKey))
            return;

        uow.Items[HandlersRegisteredKey] = true;

        uow.OnSaveChanged(async () => await FlushPendingAsync(uow));

        uow.OnCompleted(async () =>
        {
            var pending = uow.GetOrAddItem(PendingEventsKey, _ => new List<IntegrationEvent>());
            if (pending.Count > 0)
            {
                throw new InvalidOperationException(
                    $"事务已提交，但仍有 {pending.Count} 条集成事件未写入 Outbox。" +
                    "请先访问仓储/DbContext 以开启可征用的数据库事务（IOutboxEnlistableTransaction）。");
            }

            var txIds = uow.GetOrAddItem(TransactionIdsKey, _ => new HashSet<Guid>());
            if (txIds.Count == 0)
                return;

            var publisher = uow.ServiceProvider.GetRequiredService<IOutboxEventPublisher>();
            foreach (var txId in txIds)
            {
                logger.LogDebug("Dispatching outbox events for transaction {TransactionId}", txId);
                await publisher.PublishPendingAsync(txId);
            }
        });
    }

    private async Task FlushPendingAsync(IUnitOfWork uow)
    {
        var pending = uow.GetOrAddItem(PendingEventsKey, _ => new List<IntegrationEvent>());
        if (pending.Count == 0)
            return;

        var enlistable = FindEnlistableTransaction(uow);
        if (enlistable?.GetDbTransaction() == null)
        {
            throw new InvalidOperationException(
                "存在待写入 Outbox 的集成事件，但当前工作单元没有可征用的数据库事务（IOutboxEnlistableTransaction.GetDbTransaction()）。" +
                "请先访问 EF/SqlSugar 仓储，并确保与 Outbox 存储共享同一物理事务。");
        }

        foreach (var @event in pending.ToList())
            await SaveToOutboxAsync(uow, @event, enlistable);

        pending.Clear();
    }

    private async Task SaveToOutboxAsync(
        IUnitOfWork uow,
        IntegrationEvent @event,
        IOutboxEnlistableTransaction enlistable)
    {
        var dbTransaction = enlistable.GetDbTransaction()
                            ?? throw new InvalidOperationException(
                                "IOutboxEnlistableTransaction.GetDbTransaction() returned null.");

        var store = uow.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
        var envelope = EventBusMessageFactory.Create(
            @event, routingKeyResolver, enlistable.TransactionId.ToString());
        await store.EnqueueInTransactionAsync(envelope, dbTransaction);

        var txIds = uow.GetOrAddItem(TransactionIdsKey, _ => new HashSet<Guid>());
        txIds.Add(enlistable.TransactionId);

        metrics.RecordOutboxEnqueued(@event.GetType().Name);

        logger.LogInformation(
            "Outbox event {EventId} ({EventType}) enqueued for transaction {TransactionId}",
            @event.Id, @event.GetType().Name, enlistable.TransactionId);
    }

    private static IOutboxEnlistableTransaction? FindEnlistableTransaction(IUnitOfWork uow)
        => UnitOfWorkSharedDb.FindAny(uow)
           ?? uow.GetAllActiveTransactionApis().OfType<IOutboxEnlistableTransaction>().FirstOrDefault();
}
