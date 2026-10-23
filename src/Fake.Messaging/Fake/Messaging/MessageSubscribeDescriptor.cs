namespace Fake.Messaging;

/// <summary>
/// 已解析的订阅：路由在 Finder 一次算清；消费默认值由 Binder 用传输 Options 填。
/// </summary>
public sealed class MessageSubscribeDescriptor
{
    public required Type HandlerType { get; init; }

    public required Type MessageType { get; init; }

    /// <summary>路由名（RoutingKey）。</summary>
    public required string Name { get; init; }

    /// <summary>消费组（Queue）。</summary>
    public required string Group { get; init; }

    /// <summary>Topic / Exchange。</summary>
    public required string Topic { get; init; }

    /// <summary>Topic 类型（direct / topic …），已含 Messaging:DefaultTopicType。</summary>
    public required string TopicType { get; init; }

    public string? Provider { get; init; }

    public string? ConnectionName { get; init; }

    public bool AllowMissingMessageId { get; init; }
}
