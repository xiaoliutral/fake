using System.Collections.Concurrent;

namespace Fake.Messaging;

/// <summary>
/// 收集实现了 <see cref="IMessageHandle{TMessage}"/> 的具体类型（约定注册时写入）。
/// </summary>
public sealed class MessageSubscribeTypeStore
{
    private readonly ConcurrentDictionary<Type, byte> _types = new();

    public void Add(Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);
        if (implementationType.IsAbstract || implementationType.IsInterface)
            return;
        _types.TryAdd(implementationType, 0);
    }

    public IReadOnlyCollection<Type> GetTypes() => _types.Keys.ToArray();
}
