using Fake.EventBus.Distributed;
using Fake.EventBus.IntegrationEventLog.Tests.Events;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.IntegrationEventLog.Tests;

public class ApplicationEventHandlerTests
    : ApplicationTestWithTools<FakeEventBusIntegrationEventLogTestModule>
{
    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    [Fact]
    public async Task 无事务时Outbox扩展SaveEvent应失败()
    {
        var outbox = ServiceProvider.GetRequiredService<IOutboxEventLogService>();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await outbox.SaveEventAsync(new TestOutboxIntegrationEvent()));
    }
}
