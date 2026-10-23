using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// Descriptor 已含路由；本类只填 <see cref="RabbitMessageOptions"/> 消费默认值并注册。
/// </summary>
public sealed class RabbitMessageSubscribeBinder(
    IRabbitMessageConsumer consumer,
    IOptions<RabbitMessageOptions> messageOptions,
    ILogger<RabbitMessageSubscribeBinder> logger) : IMessageSubscribeBinder
{
    public const string ProviderName = "rabbit";

    public string Provider => ProviderName;

    public void Bind(MessageSubscribeDescriptor descriptor)
    {
        var o = messageOptions.Value;

        var subscription = new RabbitSubscription
        {
            Queue = descriptor.Group,
            Exchange = descriptor.Topic,
            RoutingKey = descriptor.Name,
            ExchangeType = descriptor.TopicType,
            ConnectionName = descriptor.ConnectionName ?? o.ConnectionName,
            PrefetchSize = o.PrefetchSize,
            PrefetchCount = o.PrefetchCount,
            HandlerTimeout = o.HandlerTimeout,
            HandlerRetryCount = o.HandlerRetryCount,
            EnableDlx = o.EnableDlx,
            QueueType = o.QueueType,
            DeliveryLimit = o.DeliveryLimit,
            MessageTtl = o.MessageTtl,
            QueueMaxLength = o.QueueMaxLength,
            AllowMissingMessageId = descriptor.AllowMissingMessageId || o.AllowMissingMessageId,
            UnitOfWork = o.UnitOfWork,
            Handler = BuildHandler(descriptor.HandlerType, descriptor.MessageType)
        };

        consumer.Subscribe(subscription);

        logger.LogInformation(
            "MessageSubscribe bound {Handler}<{Message}> -> rabbit:{Exchange}/{Queue} (rk={RoutingKey})",
            descriptor.HandlerType.Name, descriptor.MessageType.Name,
            descriptor.Topic, descriptor.Group, descriptor.Name);
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
