namespace Fake.EventBus.RabbitMQ;

public class RabbitMqEventBusOptions
{
    /// <summary>
    /// RabbitMQ连接名，若为空则使用默认连接<see cref="FakeRabbitMqOptions.DefaultConnectionName"/>
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// 【发布侧】连接/通道瞬时故障（BrokerUnreachable / Socket）的 Polly 重试次数，默认 10。
    /// 与 <see cref="HandlerRetryCount"/> 无关。
    /// </summary>
    public int RetryCount { get; set; } = 10;

    /// <summary>
    /// 【消费侧】Handler 执行失败时的进程内重试次数（含首次），默认 3。
    /// 全部失败后标记 Inbox=ConsumeFailed 并 Nack（通常进 DLX）。
    /// </summary>
    public int HandlerRetryCount { get; set; } = 3;

    /// <summary>
    /// 单次 Handler 执行超时。超时后取消 <see cref="CancellationToken"/>、标记失败并 Nack。
    /// 默认 5 分钟；设为 <see cref="TimeSpan.Zero"/> 关闭超时。
    /// Handler 须响应取消令牌；无视 CT 的死锁/死循环无法被真正打断。
    /// </summary>
    public TimeSpan HandlerTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public string ActivitySourceName { get; set; } = "Fake.Eventbus.RabbitMQ";

    /// <summary>
    /// 事件总线交换机名称
    /// </summary>
    public string ExchangeName { get; set; } = "Fake.Exchange.EventBus";

    /// <summary>
    /// 客户端订阅队列名称
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// 主队列与 DLX 队列类型。默认 <see cref="RabbitMqQueueType.Classic"/>。
    /// 设为 <see cref="RabbitMqQueueType.Quorum"/> 时声明 <c>x-queue-type=quorum</c>。
    /// </summary>
    public RabbitMqQueueType QueueType { get; set; } = RabbitMqQueueType.Classic;

    /// <summary>
    /// 仲裁队列投递上限（<c>x-delivery-limit</c>）。超过后按死信策略处理（通常进 DLX）, 拦截毒丸消息。
    /// 仅 <see cref="RabbitMqQueueType.Quorum"/> 时生效；≤0 表示不设置。
    /// </summary>
    public int DeliveryLimit { get; set; } = 3;

    #region Qos

    /// <summary>
    /// 消息大小限制，默认无限制
    /// </summary>
    public uint PrefetchSize { get; set; }

    /// <summary>
    /// 每次预取（未 ACK）的消息条数，同时也是消费并发上限。
    /// 消费端对 Received 立即返回并并发处理时，该值才会真正生效；为 1 时等价串行。
    /// </summary>
    public ushort PrefetchCount { get; set; } = 1;

    #endregion

    #region DLX

    /// <summary>
    /// 启动死信
    /// </summary>
    public bool EnableDlx { get; set; } = true;

    /// <summary>
    /// 设置消息过期时间，过期则自动判定为死信，默认无限制
    /// </summary>
    public int MessageTtl { get; set; }

    /// <summary>
    /// 队列最大长度，超出长度则后续消息判定为死信，默认无限制
    /// </summary>
    public int QueueMaxLength { get; set; }

    #endregion
}