namespace Fake.Messaging;

/// <summary>
/// Destination 约定：<c>eventbus:{routingKey}</c> / <c>rabbit:{exchange}/{routingKey}</c>
/// </summary>
public static class MessageDestinations
{
    public const string EventBusPrefix = "eventbus:";
    public const string RabbitPrefix = "rabbit:";

    public static string ForEventBus(string routingKey)
        => EventBusPrefix + routingKey;

    public static string ForRabbit(string exchange, string routingKey)
        => $"{RabbitPrefix}{exchange}/{routingKey}";

    public static bool IsEventBus(string destination)
        => destination.StartsWith(EventBusPrefix, StringComparison.Ordinal);

    public static bool IsRabbit(string destination)
        => destination.StartsWith(RabbitPrefix, StringComparison.Ordinal);

    public static string GetEventBusRoutingKey(string destination)
    {
        if (!IsEventBus(destination))
            throw new ArgumentException($"Not an eventbus destination: {destination}", nameof(destination));
        return destination[EventBusPrefix.Length..];
    }

    public static (string Exchange, string RoutingKey) ParseRabbit(string destination)
    {
        if (!IsRabbit(destination))
            throw new ArgumentException($"Not a rabbit destination: {destination}", nameof(destination));

        var rest = destination[RabbitPrefix.Length..];
        var slash = rest.IndexOf('/');
        if (slash <= 0 || slash >= rest.Length - 1)
            throw new ArgumentException($"Invalid rabbit destination: {destination}", nameof(destination));

        return (rest[..slash], rest[(slash + 1)..]);
    }
}
