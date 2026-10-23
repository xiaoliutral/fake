namespace Fake.Messaging;

/// <summary>
/// 订阅侧特例覆盖（可选）。路由 Name/Topic 默认跟消息 <see cref="MessageRouteAttribute"/>；
/// 消费行为（超时/重试/UoW/Prefetch/DLX）一律走传输配置（如 <c>RabbitMQ:Messaging</c>）。
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

    /// <summary>允许缺失 MessageId（外部回调）。</summary>
    public bool AllowMissingMessageId { get; set; }
}
