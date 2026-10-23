namespace Fake.Messaging;

/// <summary>
/// 声明消息发布路由。Name 为空则用消息类型名；Topic 为空则用 <see cref="MessagingOptions.DefaultTopic"/>。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class MessageRouteAttribute : Attribute
{
    public MessageRouteAttribute()
    {
    }

    public MessageRouteAttribute(string name)
    {
        Name = name;
    }

    /// <summary>路由名（Rabbit=RoutingKey）；空则类型名。</summary>
    public string? Name { get; }

    /// <summary>上游主题 / 交换机。</summary>
    public string? Topic { get; set; }

    /// <summary>主题类型（direct / topic …）。</summary>
    public string? TopicType { get; set; }

    /// <summary>true=不经 Outbox，立即投递（广播）。</summary>
    public bool Immediate { get; set; }

    /// <summary>Publisher Confirm（通常配合 Immediate）。</summary>
    public bool PublisherConfirm { get; set; }
}
