namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// 消费行为全局默认（配置节 <see cref="SectionName"/>）。
/// 路由（Topic / TopicType）不在这里，走 <c>Messaging:DefaultTopic*</c> + <see cref="MessageRouteAttribute"/>。
/// </summary>
public class RabbitMessageOptions
{
    public const string SectionName = "RabbitMQ:Messaging";

    /// <summary>RabbitMQ 连接名；空则默认连接。</summary>
    public string? ConnectionName { get; set; }

    /// <summary>Handler 失败进程内重试次数（含首次）。默认 3。</summary>
    public int HandlerRetryCount { get; set; } = 3;

    /// <summary>单次 Handler 超时。默认 5 分钟；<see cref="TimeSpan.Zero"/> 关闭。</summary>
    public TimeSpan HandlerTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>主队列与 DLX 队列类型。</summary>
    public RabbitMqQueueType QueueType { get; set; } = RabbitMqQueueType.Classic;

    /// <summary>仲裁队列 x-delivery-limit；≤0 不设置。</summary>
    public int DeliveryLimit { get; set; } = 3;

    #region Qos

    public uint PrefetchSize { get; set; }

    /// <summary>预取条数，亦为消费并发上限。</summary>
    public ushort PrefetchCount { get; set; } = 10;

    #endregion

    #region DLX

    public bool EnableDlx { get; set; } = true;

    /// <summary>消息过期（毫秒）；0=无限制。</summary>
    public int MessageTtl { get; set; }

    /// <summary>队列最大长度；0=无限制。</summary>
    public int QueueMaxLength { get; set; }

    #endregion

    /// <summary>允许缺失 MessageId（全局）；单 Handler 可用 Attribute 打开。</summary>
    public bool AllowMissingMessageId { get; set; }

    /// <summary>消费 Handler 外层 UoW。默认 NonTransactional。</summary>
    public MessageConsumeUnitOfWorkMode UnitOfWork { get; set; } = MessageConsumeUnitOfWorkMode.NonTransactional;
}
