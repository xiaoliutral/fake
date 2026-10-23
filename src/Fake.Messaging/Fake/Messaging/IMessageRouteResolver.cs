namespace Fake.Messaging;

/// <summary>
/// 统一解析 Name / Topic / TopicType（发布与订阅共用）。
/// </summary>
public interface IMessageRouteResolver
{
    /// <summary>发布：含动态路由键 <see cref="Message.GetRoutingKey"/>。</summary>
    ResolvedMessageRoute Resolve(Message message);

    /// <summary>
    /// 按消息类型解析（订阅发现）。
    /// <paramref name="nameOverride"/> 非空时覆盖 Attribute / 类型名（如 [MessageSubscribe(name)]）。
    /// </summary>
    ResolvedMessageRoute Resolve(Type messageType, string? nameOverride = null);
}

/// <summary>解析后的路由（发布 / 订阅共用）。</summary>
public sealed class ResolvedMessageRoute
{
    public required string Destination { get; init; }
    public required string Topic { get; init; }
    public required string Name { get; init; }
    public required string TopicType { get; init; }
    public bool Immediate { get; init; }
    public bool PublisherConfirm { get; init; }
}
