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
        options.EnableDlx.ShouldBeTrue();
        options.PrefetchCount.ShouldBe((ushort)1);
        options.ConnectionName.ShouldBeNull();
        options.QueueName.ShouldBeNull();
    }
}
