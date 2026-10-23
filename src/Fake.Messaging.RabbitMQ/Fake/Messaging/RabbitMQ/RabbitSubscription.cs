using Fake.UnitOfWork;

namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// 单条 Rabbit 订阅（Binder 输出：路由来自 Descriptor，消费默认来自 Options）。
/// </summary>
public sealed class RabbitSubscription
{
    public required string Queue { get; init; }

    public string Exchange { get; init; } = string.Empty;

    public string RoutingKey { get; init; } = string.Empty;

    public string ExchangeType { get; init; } = "direct";

    public string? ConnectionName { get; init; }

    public uint PrefetchSize { get; init; }

    public ushort PrefetchCount { get; init; } = 10;

    public TimeSpan HandlerTimeout { get; init; } = TimeSpan.FromMinutes(5);

    public int HandlerRetryCount { get; init; } = 3;

    public bool EnableDlx { get; init; }

    public RabbitMqQueueType QueueType { get; init; } = RabbitMqQueueType.Classic;

    public int DeliveryLimit { get; init; }

    public int MessageTtl { get; init; }

    public int QueueMaxLength { get; init; }

    public Action<IDictionary<string, object>>? Declaration { get; init; }

    public bool AllowMissingMessageId { get; init; }

    public MessageConsumeUnitOfWorkMode UnitOfWork { get; init; } = MessageConsumeUnitOfWorkMode.NonTransactional;

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
