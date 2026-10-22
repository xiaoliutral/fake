using Shouldly;

namespace Fake.EventBus.RabbitMQ.Tests;

public class RabbitMqEventBusOptionsTests
{
    [Fact]
    public void 默认选项应符合预期()
    {
        var options = new RabbitMqEventBusOptions();

        options.ExchangeName.ShouldBe("Fake.Exchange.EventBus");
        options.RetryCount.ShouldBe(10);
        options.HandlerRetryCount.ShouldBe(3);
        options.HandlerTimeout.ShouldBe(TimeSpan.FromMinutes(5));
        options.EnableDlx.ShouldBeTrue();
        options.QueueType.ShouldBe(RabbitMqQueueType.Classic);
        options.DeliveryLimit.ShouldBe(0);
        options.PrefetchCount.ShouldBe((ushort)1);
        options.ConnectionName.ShouldBeNull();
        options.QueueName.ShouldBeNull();
    }
}
