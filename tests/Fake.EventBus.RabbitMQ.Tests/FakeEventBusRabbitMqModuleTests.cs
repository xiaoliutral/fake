using Fake.EventBus.Distributed;
using Fake.EventBus.Local;
using Fake.EventBus.RabbitMQ.Tests.Events;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Fake.EventBus.RabbitMQ.Tests;

public class FakeEventBusRabbitMqModuleTests : ApplicationTest<FakeEventBusRabbitMqTestModule>
{
    [Fact]
    public void 应注册分布式事件总线为RabbitMqEventBus且本地总线保持LocalEventBus()
    {
        var distributedEventBus = ServiceProvider.GetRequiredService<IDistributedEventBus>();
        var physicalEventBus = ServiceProvider.GetRequiredService<IPhysicalDistributedEventBus>();
        var eventBus = ServiceProvider.GetRequiredService<IEventBus>();
        var localEventBus = ServiceProvider.GetRequiredService<ILocalEventBus>();

        distributedEventBus.ShouldBeOfType<RabbitMqEventBus>();
        physicalEventBus.ShouldBeOfType<RabbitMqEventBus>();
        eventBus.ShouldBeOfType<LocalEventBus>();
        localEventBus.ShouldBeOfType<LocalEventBus>();
        ReferenceEquals(eventBus, localEventBus).ShouldBeTrue();
        ReferenceEquals(distributedEventBus, ServiceProvider.GetRequiredService<RabbitMqEventBus>()).ShouldBeTrue();
        ReferenceEquals(physicalEventBus, distributedEventBus).ShouldBeTrue();
    }

    [Fact]
    public void 应注册为HostedService()
    {
        var hostedServices = ServiceProvider.GetServices<IHostedService>().ToList();

        hostedServices.ShouldContain(x => x is RabbitMqEventBus);
    }

    [Fact]
    public void 应收集集成事件处理器对应的事件类型()
    {
        var options = ServiceProvider.GetRequiredService<IOptions<EventBusSubscriptionOptions>>().Value;

        options.EventTypes.ShouldContainKey(nameof(SimpleIntegrationEvent));
        options.EventTypes[nameof(SimpleIntegrationEvent)].ShouldBe(typeof(SimpleIntegrationEvent));
    }

    [Fact]
    public void 应从配置绑定EventBus选项()
    {
        var options = ServiceProvider.GetRequiredService<IOptions<RabbitMqEventBusOptions>>().Value;

        options.ExchangeName.ShouldBe("Fake.Exchange.EventBus.Tests");
        options.QueueName.ShouldBe("Fake.EventBus.RabbitMQ.Tests");
        options.RetryCount.ShouldBe(3);
        options.EnableDlx.ShouldBeFalse();
    }
}
