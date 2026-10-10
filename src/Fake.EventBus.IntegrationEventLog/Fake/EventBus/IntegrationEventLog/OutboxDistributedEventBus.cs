using Fake.EventBus.Distributed;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.IntegrationEventLog;

/// <summary>
/// 默认 Outbox 总线（业务无感，ORM 无关）：
/// 事务性 UoW 内只写 Outbox；Commit 后立刻投递；后台扫描兜底。
/// 事务来自 <see cref="IOutboxEnlistableTransaction"/>（EF / SqlSugar 等）。
/// </summary>
public class OutboxDistributedEventBus(
    IUnitOfWorkManager unitOfWorkManager,
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
            throw new InvalidOperationException(
                "集成事件必须在事务性工作单元内发布（写入 Outbox）。" +
                "请使用 [UnitOfWork] / 事务性 UoW；若需绕过 Outbox 直发，请注入 IPhysicalDistributedEventBus。");
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
        {
            await SaveToOutboxAsync(uow, @event, enlistable);
        }

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

        var outbox = uow.ServiceProvider.GetRequiredService<IOutboxEventLogService>();
        await outbox.SaveEventAsync(@event, new DbTransactionContext(enlistable.TransactionId, dbTransaction));

        var txIds = uow.GetOrAddItem(TransactionIdsKey, _ => new HashSet<Guid>());
        txIds.Add(enlistable.TransactionId);

        logger.LogInformation(
            "Outbox event {EventId} ({EventType}) enqueued for transaction {TransactionId}",
            @event.Id, @event.GetType().Name, enlistable.TransactionId);
    }

    /// <summary>
    /// 优先选择已暴露 ADO.NET 事务的征用点（EF / Sugar 混用时取第一个可用）。
    /// </summary>
    private static IOutboxEnlistableTransaction? FindEnlistableTransaction(IUnitOfWork uow)
    {
        var all = uow.GetAllActiveTransactionApis().OfType<IOutboxEnlistableTransaction>().ToList();
        return all.FirstOrDefault(t => t.GetDbTransaction() != null) ?? all.FirstOrDefault();
    }
}
