using System.Collections.Concurrent;
using System.Reflection;
using Fake.EventBus.Distributed;

namespace Fake.EventBus.IntegrationEventLog;

internal static class IntegrationEventTypeResolver
{
    private static readonly ConcurrentDictionary<string, Type> Cache = new();

    public static Type Resolve(string eventTypeName)
    {
        return Cache.GetOrAdd(eventTypeName, static name =>
        {
            var shortName = name.Contains('.') ? name.Split('.').Last() : name;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? type = null;
                try
                {
                    type = assembly.GetType(name, throwOnError: false, ignoreCase: false);
                    if (type == null || !typeof(IntegrationEvent).IsAssignableFrom(type))
                    {
                        type = GetLoadableTypes(assembly)
                            .FirstOrDefault(t =>
                                typeof(IntegrationEvent).IsAssignableFrom(t) &&
                                (t.FullName == name || t.Name == shortName));
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // ignored — try next assembly
                }

                if (type != null && typeof(IntegrationEvent).IsAssignableFrom(type) && !type.IsAbstract)
                    return type;
            }

            throw new FakeException($"非法的事件类型：{name}");
        });
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }
}
