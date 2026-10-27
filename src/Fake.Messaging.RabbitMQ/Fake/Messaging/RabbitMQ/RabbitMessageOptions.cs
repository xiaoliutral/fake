namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// 消费行为（配置节 <see cref="SectionName"/>）。经 <c>IOptionsMonitor</c> 绑定，支持热更新。
/// 路由（Topic / TopicType）走 <c>Messaging:DefaultTopic*</c> + <see cref="MessageRouteAttribute"/>。
/// </summary>
public class RabbitMessageOptions
{
    public const string SectionName = "RabbitMQ:Messaging";

    /// <summary>RabbitMQ 连接名；空则默认连接。</summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// 【发布侧】连接/通道瞬时故障（BrokerUnreachable / Socket）的 Polly 重试次数，默认 10。
    /// 对齐 EventBus <c>RabbitMqEventBusOptions.RetryCount</c>；与 <see cref="HandlerRetryCount"/> 无关。
    /// </summary>
    public int RetryCount { get; set; } = 10;

    /// <summary>
    /// 【消费侧】Handler 失败进程内重试次数（含首次），默认 3。
    /// 对齐 EventBus <c>HandlerRetryCount</c>。
    /// </summary>
    public int HandlerRetryCount { get; set; } = 3;

    /// <summary>单次 Handler 超时。默认 5 分钟；<see cref="TimeSpan.Zero"/> 关闭。</summary>
    public TimeSpan HandlerTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>主队列与 DLX 队列类型。</summary>
    public RabbitMqQueueType QueueType { get; set; } = RabbitMqQueueType.Classic;

    /// <summary>仲裁队列 x-delivery-limit；≤0 不设置。</summary>
    public int DeliveryLimit { get; set; } = 3;

    #region Qos

    public uint PrefetchSize { get; set; }

    /// <summary>
    /// 全局预取/并发上限（BasicQos，且本实现 fire-and-forget 派发时≈最大并发 Handler 数）。
    /// <c>0</c>=自动 <see cref="Environment.ProcessorCount"/>（对齐 CAP 并行订阅默认线程数）。
    /// 可被 Attribute / <see cref="Handlers"/> 覆盖。
    /// </summary>
    public ushort PrefetchCount { get; set; }

    #endregion

    #region DLX

    public bool EnableDlx { get; set; } = true;

    /// <summary>消息过期（毫秒）；0=无限制。</summary>
    public int MessageTtl { get; set; }

    /// <summary>队列最大长度；0=无限制。</summary>
    public int QueueMaxLength { get; set; }

    #endregion

    /// <summary>消费 Handler 外层 UoW。默认 NonTransactional。</summary>
    public MessageConsumeUnitOfWorkMode UnitOfWork { get; set; } = MessageConsumeUnitOfWorkMode.NonTransactional;

    /// <summary>
    /// 按 Handler 短名 / 全名 / Queue 名覆盖并发等。
    /// 例：<c>"TrackUpsertEventHandler": { "PrefetchCount": 5 }</c>
    /// </summary>
    public Dictionary<string, RabbitMessageHandlerOptions> Handlers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>单 Handler / 队列消费覆盖（可热更新）。</summary>
public class RabbitMessageHandlerOptions
{
    /// <summary>覆盖该 Handler 的 PrefetchCount；null=不覆盖。</summary>
    public ushort? PrefetchCount { get; set; }

    /// <summary>覆盖重试次数；null=不覆盖。</summary>
    public int? HandlerRetryCount { get; set; }

    /// <summary>覆盖超时；null=不覆盖。</summary>
    public TimeSpan? HandlerTimeout { get; set; }
}
