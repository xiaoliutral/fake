using Fake.EventBus.Distributed;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Fake.EventBus.Tests;

public class EventRoutingKeyResolverTests
{
    private sealed class OrderCreatedEvent : IntegrationEvent;

    [Fact]
    public void 默认ShortName()
    {
        var resolver = Create(new EventRoutingOptions());
        resolver.Resolve(typeof(OrderCreatedEvent)).ShouldBe(nameof(OrderCreatedEvent));
    }

    [Fact]
    public void FullName策略()
    {
        var resolver = Create(new EventRoutingOptions { Strategy = EventRoutingKeyStrategy.FullName });
        resolver.Resolve(typeof(OrderCreatedEvent)).ShouldBe(typeof(OrderCreatedEvent).FullName);
    }

    [Fact]
    public void CustomRoutes优先于策略_按ShortName()
    {
        var resolver = Create(new EventRoutingOptions
        {
            Strategy = EventRoutingKeyStrategy.FullName,
            CustomRoutes = { [nameof(OrderCreatedEvent)] = "orders.created" }
        });

        resolver.Resolve(typeof(OrderCreatedEvent)).ShouldBe("orders.created");
    }

    [Fact]
    public void CustomRoutes按FullName()
    {
        var resolver = Create(new EventRoutingOptions
        {
            CustomRoutes = { [typeof(OrderCreatedEvent).FullName!] = "orders.created.v2" }
        });

        resolver.Resolve(typeof(OrderCreatedEvent)).ShouldBe("orders.created.v2");
    }

    private static DefaultEventRoutingKeyResolver Create(EventRoutingOptions options)
        => new(Options.Create(options));
}
