using Microsoft.Extensions.Options;

namespace Fake.EventBus.Distributed;

public class DefaultEventRoutingKeyResolver(IOptions<EventRoutingOptions> options) : IEventRoutingKeyResolver
{
    public string Resolve(Type eventType)
    {
        var o = options.Value;

        if (TryGetCustomRoute(o.CustomRoutes, eventType.FullName, out var byFull))
            return byFull;

        if (TryGetCustomRoute(o.CustomRoutes, eventType.Name, out var byShort))
            return byShort;

        return o.Strategy == EventRoutingKeyStrategy.FullName
            ? eventType.FullName ?? eventType.Name
            : eventType.Name;
    }

    private static bool TryGetCustomRoute(
        IDictionary<string, string> routes,
        string? key,
        out string routingKey)
    {
        routingKey = string.Empty;
        if (string.IsNullOrEmpty(key) || routes.Count == 0)
            return false;

        if (routes.TryGetValue(key, out var exact) && !string.IsNullOrWhiteSpace(exact))
        {
            routingKey = exact;
            return true;
        }

        foreach (var pair in routes)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(pair.Value))
            {
                routingKey = pair.Value;
                return true;
            }
        }

        return false;
    }
}
