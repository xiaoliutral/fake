using System.Reflection;
using Microsoft.Extensions.Options;

namespace Fake.Messaging;

/// <summary>
/// 路由唯一来源：动态键 → nameOverride → [MessageRoute] → 类型名；
/// Topic / TopicType → [MessageRoute] → <see cref="MessagingOptions"/>（<c>IOptionsMonitor</c>，可热更新）。
/// </summary>
public sealed class DefaultMessageRouteResolver(IOptionsMonitor<MessagingOptions> options) : IMessageRouteResolver
{
    public ResolvedMessageRoute Resolve(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return ResolveCore(message.GetType(), nameOverride: null, dynamicKey: message.GetRoutingKey());
    }

    public ResolvedMessageRoute Resolve(Type messageType, string? nameOverride = null)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        if (!typeof(Message).IsAssignableFrom(messageType))
            throw new ArgumentException($"Type {messageType.Name} must inherit Message.", nameof(messageType));

        return ResolveCore(messageType, nameOverride, dynamicKey: null);
    }

    private ResolvedMessageRoute ResolveCore(Type messageType, string? nameOverride, string? dynamicKey)
    {
        var attr = messageType.GetCustomAttribute<MessageRouteAttribute>(inherit: true);
        var o = options.CurrentValue;

        var name = FirstNonEmpty(dynamicKey, nameOverride, attr?.Name) ?? messageType.Name;
        var topic = FirstNonEmpty(attr?.Topic, o.DefaultTopic)
                    ?? throw new InvalidOperationException(
                        $"Message {messageType.Name} has no Topic; set [MessageRoute(Topic=...)] or Messaging:DefaultTopic.");
        var topicType = FirstNonEmpty(attr?.TopicType, o.DefaultTopicType) ?? "direct";

        return new ResolvedMessageRoute
        {
            Destination = MessageDestinations.ForRabbit(topic, name),
            Topic = topic,
            Name = name,
            TopicType = topicType,
            Immediate = attr?.Immediate ?? false,
            PublisherConfirm = attr?.PublisherConfirm ?? false
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrEmpty(v))
                return v;
        }

        return null;
    }
}
