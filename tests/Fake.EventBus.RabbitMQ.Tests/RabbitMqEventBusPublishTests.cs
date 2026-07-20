using Fake.EventBus.Distributed;
using Fake.EventBus.RabbitMQ.Tests.Events;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Fake.EventBus.RabbitMQ.Tests;

/// <summary>
/// 依赖真实 RabbitMQ；本地无 Broker 时会失败。
/// </summary>
public class RabbitMqEventBusPublishTests : ApplicationTest<FakeEventBusRabbitMqTestModule>
{
    [Fact]
    public async Task 发布后消费者应处理事件()
    {
        SimpleIntegrationEventHandler.Init();

        var bus = ServiceProvider.GetRequiredService<RabbitMqEventBus>();
        await bus.StartAsync(CancellationToken.None);

        // 等待消费者通道与队列绑定就绪
        await Task.Delay(1000);

        await bus.PublishAsync(new SimpleIntegrationEvent { Num = 42 }, CancellationToken.None);

        await WaitUntilAsync(() => SimpleIntegrationEventHandler.HandleCount >= 1, TimeSpan.FromSeconds(10));

        SimpleIntegrationEventHandler.HandleCount.ShouldBeGreaterThanOrEqualTo(1);

        await bus.StopAsync(CancellationToken.None);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(200);
        }

        condition().ShouldBeTrue("Timed out waiting for event handler.");
    }
}
