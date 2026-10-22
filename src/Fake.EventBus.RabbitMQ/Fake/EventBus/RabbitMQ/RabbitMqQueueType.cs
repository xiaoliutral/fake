namespace Fake.EventBus.RabbitMQ;

/// <summary>
/// RabbitMQ 队列类型（声明时通过 <c>x-queue-type</c> 指定，创建后不可改）。
/// </summary>
public enum RabbitMqQueueType
{
    /// <summary>经典队列（默认，兼容旧部署）。</summary>
    Classic = 0,

    /// <summary>
    /// 仲裁队列：多节点复制、更高可用。须 durable、非 exclusive；
    /// 与已存在的 classic 同名队列互斥，切换需删队列或换名。支持最少一次死信
    /// </summary>
    Quorum = 1
}
