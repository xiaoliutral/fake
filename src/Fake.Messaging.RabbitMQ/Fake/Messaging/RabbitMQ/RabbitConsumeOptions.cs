using RabbitMQ.Client;

namespace Fake.Messaging.RabbitMQ;

public sealed class RabbitConsumeOptions
{
    public required string Queue { get; init; }

    public string Exchange { get; init; } = string.Empty;

    public string RoutingKey { get; init; } = string.Empty;

    public string? ConnectionName { get; init; }

    /// <summary>Prefetch = 并发上限。</summary>
    public ushort FetchCount { get; init; } = 10;

    public TimeSpan HandlerTimeout { get; init; } = TimeSpan.FromMinutes(5);

    public int HandlerRetryCount { get; init; } = 1;

    public bool EnableDlx { get; init; }

    public RabbitMqQueueType QueueType { get; init; } = RabbitMqQueueType.Classic;

    /// <summary>仅 Quorum：x-delivery-limit；≤0 不设置。</summary>
    public int DeliveryLimit { get; init; }

    public Action<IDictionary<string, object>>? Declaration { get; init; }

    public required Func<RabbitConsumeContext, CancellationToken, Task> Handler { get; init; }
}

public enum RabbitMqQueueType
{
    Classic = 0,
    Quorum = 1
}

public sealed class RabbitConsumeContext
{
    public required Guid MessageId { get; init; }
    public required string Queue { get; init; }
    public required string RoutingKey { get; init; }
    public required ReadOnlyMemory<byte> Body { get; init; }
    public required IReadOnlyDictionary<string, string> Headers { get; init; }
    public bool Redelivered { get; init; }
}
