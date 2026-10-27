namespace Fake.Messaging;

/// <summary>
/// 订阅侧特例覆盖（可选）。路由 Name/Topic 默认跟消息 <see cref="MessageRouteAttribute"/>；
/// 超时/重试/UoW/DLX 等走传输配置；<see cref="PrefetchCount"/> 可按队列覆盖并发。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class MessageSubscribeAttribute : Attribute
{
    public MessageSubscribeAttribute()
    {
    }

    public MessageSubscribeAttribute(string name)
    {
        Name = name;
    }

    /// <summary>覆盖路由名 / 默认队列名；空则用消息 Route / 类型名。</summary>
    public string? Name { get; }

    /// <summary>消费组（Queue）；缺省等于 Name。</summary>
    public string? Group { get; set; }

    /// <summary>传输 Provider（多 Binder 时必填）；如 <c>rabbit</c>。</summary>
    public string? Provider { get; set; }

    /// <summary>连接名；空则传输默认连接。</summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// 本队列预取/并发上限（BasicQos）。0=使用传输全局默认
    ///（<c>RabbitMQ:Messaging:PrefetchCount</c>，其中 0 再回落为 <c>Environment.ProcessorCount</c>）。
    /// </summary>
    public ushort PrefetchCount { get; set; }

    /// <summary>
    /// 外部队列（如接口通回调）：允许缺失 MessageId，且不声明 DLX（对接已有拓扑）。
    /// 内部队列保持默认：必有 MessageId，DLX 跟随 <c>RabbitMQ:Messaging:EnableDlx</c>。
    /// </summary>
    public bool ExternalQueue { get; set; }
}
