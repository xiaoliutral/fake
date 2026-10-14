using Fake.EventBus.Distributed;
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
                .Select(h => h.GetType().FullName ?? h.GetType().Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            if (handlers.Count == 0)
            {
                var generic = typeof(IEventHandler<>).MakeGenericType(eventType);
                handlers = scope.ServiceProvider.GetServices(generic)
                    .Select(h => h!.GetType().FullName ?? h.GetType().Name)
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
}
