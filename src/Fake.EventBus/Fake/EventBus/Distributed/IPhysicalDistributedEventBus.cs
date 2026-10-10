namespace Fake.EventBus.Distributed;

/// <summary>
/// 物理总线（如 RabbitMQ），供 Outbox 在 Commit 后真实投递。
/// 业务代码应注入 <see cref="IDistributedEventBus"/>，不要直接依赖本接口。
/// </summary>
public interface IPhysicalDistributedEventBus : IDistributedEventBus;
