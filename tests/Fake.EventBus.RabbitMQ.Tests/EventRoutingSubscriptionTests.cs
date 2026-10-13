using Fake.EventBus.Distributed;
using Fake.EventBus.RabbitMQ.Tests.Events;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Fake.EventBus.RabbitMQ.Tests;

public class EventRoutingSubscriptionTests : ApplicationTest<FakeEventBusRabbitMqTestModule>
{
    [Fact]
    public void 自定义路由解析应覆盖ShortName()
    {
        var options = ServiceProvider.GetRequiredService<IOptions<EventRoutingOptions>>();
        options.Value.CustomRoutes[nameof(SimpleIntegrationEvent)] = "simple.integration.custom";

        var resolver = ServiceProvider.GetRequiredService<IEventRoutingKeyResolver>();
        resolver.Resolve(typeof(SimpleIntegrationEvent)).ShouldBe("simple.integration.custom");
    }

    [Fact]
    public void MessageHeaders常量应稳定()
    {
        EventBusMessageHeaders.EventId.ShouldBe("fake-event-id");
        EventBusMessageHeaders.EventType.ShouldBe("fake-event-type");
    }
}
