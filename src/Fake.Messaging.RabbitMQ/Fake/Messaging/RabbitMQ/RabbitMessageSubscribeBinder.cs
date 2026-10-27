using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// 绑定路由 + Handler；消费运行时参数走 <see cref="IOptionsMonitor{RabbitMessageOptions}"/>。
/// </summary>
public sealed class RabbitMessageSubscribeBinder(
    IRabbitMessageConsumer consumer,
    IOptionsMonitor<RabbitMessageOptions> messageOptions,
    ILogger<RabbitMessageSubscribeBinder> logger) : IMessageSubscribeBinder
{
    public const string ProviderName = "rabbit";

    public string Provider => ProviderName;

    public void Bind(MessageSubscribeDescriptor descriptor)
    {
        var o = messageOptions.CurrentValue;

        var subscription = new RabbitSubscription
        {
            Queue = descriptor.Group,
            Exchange = descriptor.Topic,
            RoutingKey = descriptor.Name,
            ExchangeType = descriptor.TopicType,
            ConnectionName = descriptor.ConnectionName ?? o.ConnectionName,
            HandlerTypeName = descriptor.HandlerType.Name,
            HandlerTypeFullName = descriptor.HandlerType.FullName,
            AttributePrefetchCount = descriptor.PrefetchCount,
            ExternalQueue = descriptor.ExternalQueue,
            EnableDlx = descriptor.ExternalQueue ? false : o.EnableDlx,
            QueueType = o.QueueType,
            DeliveryLimit = o.DeliveryLimit,
            MessageTtl = o.MessageTtl,
            QueueMaxLength = o.QueueMaxLength,
            Handler = BuildHandler(descriptor.HandlerType, descriptor.MessageType)
        };

        var prefetch = RabbitMessageOptionsResolver.ResolvePrefetch(o, subscription);
        consumer.Subscribe(subscription);

        logger.LogInformation(
            "MessageSubscribe bound {Handler}<{Message}> -> rabbit:{Exchange}/{Queue} (rk={RoutingKey}, prefetch={Prefetch})",
            descriptor.HandlerType.Name, descriptor.MessageType.Name,
            descriptor.Topic, descriptor.Group, descriptor.Name, prefetch);
    }

    private static Func<RabbitConsumeContext, CancellationToken, Task> BuildHandler(
        Type handlerType,
        Type messageType)
    {
        var handleIface = typeof(IMessageHandle<>).MakeGenericType(messageType);
        var handleMethod = handleIface.GetMethod(
                               nameof(IMessageHandle<Message>.HandleAsync),
                               [messageType, typeof(CancellationToken)])
                           ?? throw new InvalidOperationException(
                               $"HandleAsync not found on IMessageHandle<{messageType.Name}>.");

        return async (ctx, ct) =>
        {
            var instance = ctx.Services.GetRequiredService(handlerType);
            var serializer = ctx.Services.GetRequiredService<IMessageBodySerializer>();
            var message = serializer.Deserialize(ctx.Body.Span, messageType)
                          ?? throw new InvalidOperationException(
                              $"Cannot deserialize {messageType.Name} for queue {ctx.Queue}");

            var result = handleMethod.Invoke(instance, [message, ct]);
            if (result is Task task)
                await task.ConfigureAwait(false);
        };
    }
}
