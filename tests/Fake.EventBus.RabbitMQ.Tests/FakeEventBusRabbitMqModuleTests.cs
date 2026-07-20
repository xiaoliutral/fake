using Fake.EventBus.Distributed;
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
    public void 应注册分布式事件总线为RabbitMqEventBus()
    {
        var distributedEventBus = ServiceProvider.GetRequiredService<IDistributedEventBus>();
        var eventBus = ServiceProvider.GetRequiredService<IEventBus>();

        distributedEventBus.ShouldBeOfType<RabbitMqEventBus>();
        eventBus.ShouldBeOfType<RabbitMqEventBus>();
        ReferenceEquals(distributedEventBus, eventBus).ShouldBeTrue();
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
