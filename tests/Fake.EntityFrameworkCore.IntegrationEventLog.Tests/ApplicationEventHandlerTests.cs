using Application.IntegrationEvents;
using Fake.EventBus.Distributed;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.EntityFrameworkCore.IntegrationEventLog.Tests;

public class ApplicationEventHandlerTests
    : ApplicationTestWithTools<FakeEntityFrameworkCoreIntegrationEventLogTestModule>
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
        var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(TestDataBuilder.UserId);
        await _outboxEventLogService.SaveEventAsync(orderStartedIntegrationEvent);
    }
}