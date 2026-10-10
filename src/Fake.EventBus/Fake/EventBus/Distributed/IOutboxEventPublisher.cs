namespace Fake.EventBus.Distributed;

/// <summary>
/// 事务提交后立即发布该事务写入的 Outbox 事件。
/// 默认由 <c>OutboxDistributedEventBus</c> 在 UoW.OnCompleted 中调用；也可显式调用（eShop 风格）。
/// 后台扫描仅作失败/崩溃兜底。
/// </summary>
public interface IOutboxEventPublisher
{
    /// <summary>
    /// 发布指定事务下尚未发送的 Outbox 事件。须在业务事务 Commit 之后调用。
    /// </summary>
    Task PublishPendingAsync(Guid transactionId, CancellationToken cancellationToken = default);
}
