using System.Transactions;
using Application.IntegrationEvents;
using Fake.EventBus.Distributed;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.EventBus.IntegrationEventLog.Tests;

public class ApplicationEventHandlerTests
    : ApplicationTestWithTools<FakeEventBusIntegrationEventLogTestModule>
{
    private readonly IOutboxEventLogService _outboxEventLogService;

    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    public ApplicationEventHandlerTests()
    {
        _outboxEventLogService = ServiceProvider.GetRequiredService<IOutboxEventLogService>();
    }

    [Fact]
    async Task 发布集成日志()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(TestDataBuilder.UserId);
            await _outboxEventLogService.SaveEventAsync(orderStartedIntegrationEvent);
        });
    }
}