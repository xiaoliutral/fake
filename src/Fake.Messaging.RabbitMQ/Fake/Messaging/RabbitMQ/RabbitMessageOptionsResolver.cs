namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// Prefetch / 超时 / 重试解析：Attribute → Handlers[名] → 全局。
/// </summary>
internal static class RabbitMessageOptionsResolver
{
    public static ushort ResolvePrefetch(RabbitMessageOptions options, RabbitSubscription subscription)
    {
        if (subscription.AttributePrefetchCount != 0)
            return subscription.AttributePrefetchCount;

        if (TryGetHandler(options, subscription, out var handler) &&
            handler.PrefetchCount is { } p && p > 0)
            return p;

        return options.PrefetchCount == 0 ? (ushort)1 : options.PrefetchCount;
    }

    public static int ResolveHandlerRetryCount(RabbitMessageOptions options, RabbitSubscription subscription)
    {
        if (TryGetHandler(options, subscription, out var handler) &&
            handler.HandlerRetryCount is { } r && r > 0)
            return r;

        return Math.Max(1, options.HandlerRetryCount);
    }

    public static TimeSpan ResolveHandlerTimeout(RabbitMessageOptions options, RabbitSubscription subscription)
    {
        if (TryGetHandler(options, subscription, out var handler) && handler.HandlerTimeout.HasValue)
            return handler.HandlerTimeout.Value;

        return options.HandlerTimeout;
    }

    public static bool ResolveAllowMissingMessageId(RabbitMessageOptions options, RabbitSubscription subscription)
        => subscription.AttributeAllowMissingMessageId || options.AllowMissingMessageId;

    public static MessageConsumeUnitOfWorkMode ResolveUnitOfWork(RabbitMessageOptions options)
        => options.UnitOfWork;

    private static bool TryGetHandler(
        RabbitMessageOptions options,
        RabbitSubscription subscription,
        out RabbitMessageHandlerOptions handler)
    {
        handler = null!;
        if (options.Handlers.Count == 0)
            return false;

        if (!string.IsNullOrEmpty(subscription.HandlerTypeFullName) &&
            options.Handlers.TryGetValue(subscription.HandlerTypeFullName!, out handler!))
            return true;

        if (options.Handlers.TryGetValue(subscription.HandlerTypeName, out handler!))
            return true;

        if (options.Handlers.TryGetValue(subscription.Queue, out handler!))
            return true;

        return false;
    }
}
