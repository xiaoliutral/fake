using Fake.EventBus.Distributed;
using Fake.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fake.EventBus.RabbitMQ;

/// <summary>
/// 从订阅表与 DI keyed handlers 汇总当前进程事件拓扑。
/// </summary>
public class RabbitMqEventBusTopology(
    IOptions<EventBusSubscriptionOptions> subscriptionOptions,
    IEventRoutingKeyResolver routingKeyResolver,
    IServiceProvider serviceProvider) : IEventBusTopology
{
    public IReadOnlyList<EventSubscriptionDto> GetSubscriptions()
    {
        var eventTypes = subscriptionOptions.Value.EventTypes.Values
            .Distinct()
            .OrderBy(t => t.FullName)
            .ToList();

        using var scope = serviceProvider.CreateScope();
        var list = new List<EventSubscriptionDto>(eventTypes.Count);

        foreach (var eventType in eventTypes)
        {
            var handlers = scope.ServiceProvider.GetKeyedServices<IEventHandler>(eventType)
                .Select(FormatHandlerType)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            if (handlers.Count == 0)
            {
                var generic = typeof(IEventHandler<>).MakeGenericType(eventType);
                handlers = scope.ServiceProvider.GetServices(generic)
                    .Where(h => h != null)
                    .Select(h => FormatHandlerType(h!))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();
            }

            list.Add(new EventSubscriptionDto(
                eventType.FullName ?? eventType.Name,
                eventType.Name,
                routingKeyResolver.Resolve(eventType),
                handlers));
        }

        return list;
    }

    /// <summary>
    /// DI 解析出的 Handler 常被 Castle 代理包装，展示真实实现类型。
    /// </summary>
    private static string FormatHandlerType(object handler)
    {
        var type = ProxyHelper.GetUnProxiedType(handler);
        return type.FullName ?? type.Name;
    }
}
