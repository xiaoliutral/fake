using Fake.UnitOfWork;

namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// 单条 Rabbit 订阅。拓扑在绑定时装定；Prefetch/超时/重试等运行时经 <see cref="IOptionsMonitor{RabbitMessageOptions}"/> 解析（可热更新）。
/// </summary>
public sealed class RabbitSubscription
{
    public required string Queue { get; init; }

    public string Exchange { get; init; } = string.Empty;

    public string RoutingKey { get; init; } = string.Empty;

    public string ExchangeType { get; init; } = "direct";

    public string? ConnectionName { get; init; }

    /// <summary>Handler 短名，用于 <c>RabbitMQ:Messaging:Handlers</c> 匹配。</summary>
    public required string HandlerTypeName { get; init; }

    /// <summary>Handler 全名，优先匹配配置。</summary>
    public string? HandlerTypeFullName { get; init; }

    /// <summary>Attribute 覆盖；0=走配置 / 全局。</summary>
    public ushort AttributePrefetchCount { get; init; }

    public bool AttributeAllowMissingMessageId { get; init; }

    /// <summary>声明期快照（改 DLX/队列类型需重启进程）。</summary>
    public bool EnableDlx { get; init; }

    public RabbitMqQueueType QueueType { get; init; } = RabbitMqQueueType.Classic;

    public int DeliveryLimit { get; init; }

    public int MessageTtl { get; init; }

    public int QueueMaxLength { get; init; }

    public Action<IDictionary<string, object>>? Declaration { get; init; }

    public UnitOfWorkAttribute? UnitOfWorkAttribute { get; init; }

    public required Func<RabbitConsumeContext, CancellationToken, Task> Handler { get; init; }
}

public sealed class RabbitConsumeContext
{
    public required Guid MessageId { get; init; }
    public required string Queue { get; init; }
    public required string RoutingKey { get; init; }
    public required ReadOnlyMemory<byte> Body { get; init; }
    public required IReadOnlyDictionary<string, string> Headers { get; init; }
    public bool Redelivered { get; init; }

    /// <summary>当前消费 scope 的 <see cref="IServiceProvider"/>（Inbox 与 Handler 共用）。</summary>
    public required IServiceProvider Services { get; init; }
}
