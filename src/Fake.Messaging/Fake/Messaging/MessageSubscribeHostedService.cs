using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fake.Messaging;

/// <summary>
/// 启动时：Finder 发现 → Binder 绑定。须在各传输 Consumer HostedService 之前注册。
/// </summary>
public sealed class MessageSubscribeHostedService(
    MessageSubscribeFinder finder,
    IEnumerable<IMessageSubscribeBinder> binders,
    ILogger<MessageSubscribeHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var binderList = binders.ToList();
        if (binderList.Count == 0)
            return Task.CompletedTask;

        foreach (var descriptor in finder.FindAll())
        {
            var binder = ResolveBinder(binderList, descriptor);
            binder.Bind(descriptor);
            logger.LogDebug(
                "Subscribed {Handler}<{Message}> name={Name} topic={Topic}",
                descriptor.HandlerType.Name, descriptor.MessageType.Name,
                descriptor.Name, descriptor.Topic);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IMessageSubscribeBinder ResolveBinder(
        IReadOnlyList<IMessageSubscribeBinder> binders,
        MessageSubscribeDescriptor descriptor)
    {
        if (!string.IsNullOrWhiteSpace(descriptor.Provider))
        {
            return binders.FirstOrDefault(b =>
                       string.Equals(b.Provider, descriptor.Provider, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException(
                       $"No IMessageSubscribeBinder for Provider '{descriptor.Provider}'.");
        }

        if (binders.Count == 1)
            return binders[0];

        throw new InvalidOperationException(
            $"Multiple binders; set MessageSubscribe.Provider on {descriptor.HandlerType.Name}.");
    }
}
