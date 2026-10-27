using System.Reflection;

namespace Fake.Messaging;

/// <summary>
/// 发现 <see cref="IMessageHandle{TMessage}"/>，用 <see cref="IMessageRouteResolver"/> 解析路由。
/// 不碰传输消费默认值（超时/重试等由 Binder + Options 负责）。
/// </summary>
public sealed class MessageSubscribeFinder(
    MessageSubscribeTypeStore typeStore,
    IMessageRouteResolver routeResolver)
{
    public IReadOnlyList<MessageSubscribeDescriptor> FindAll()
    {
        var list = new List<MessageSubscribeDescriptor>();
        foreach (var type in typeStore.GetTypes())
            list.AddRange(FindType(type));
        return list;
    }

    private IEnumerable<MessageSubscribeDescriptor> FindType(Type handlerType)
    {
        var handleIfaces = handlerType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandle<>))
            .ToArray();
        if (handleIfaces.Length == 0)
            return [];

        var attr = handlerType.GetCustomAttribute<MessageSubscribeAttribute>(inherit: true);
        // 多消息处理器：类上 Name 不套到每个 T
        var applyClassName = handleIfaces.Length == 1;

        var list = new List<MessageSubscribeDescriptor>(handleIfaces.Length);
        foreach (var iface in handleIfaces)
        {
            var messageType = iface.GetGenericArguments()[0];
            var nameOverride = applyClassName ? attr?.Name : null;
            var route = routeResolver.Resolve(messageType, nameOverride);

            list.Add(new MessageSubscribeDescriptor
            {
                HandlerType = handlerType,
                MessageType = messageType,
                Name = route.Name,
                Group = string.IsNullOrEmpty(attr?.Group) ? route.Name : attr!.Group!,
                Topic = route.Topic,
                TopicType = route.TopicType,
                Provider = attr?.Provider,
                ConnectionName = attr?.ConnectionName,
                PrefetchCount = attr?.PrefetchCount ?? 0,
                AllowMissingMessageId = attr?.AllowMissingMessageId ?? false
            });
        }

        return list;
    }
}
