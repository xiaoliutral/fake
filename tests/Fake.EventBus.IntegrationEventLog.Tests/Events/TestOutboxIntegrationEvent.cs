using Fake.EventBus.Distributed;

namespace Fake.EventBus.IntegrationEventLog.Tests.Events;

public class TestOutboxIntegrationEvent : IntegrationEvent
{
    public string Payload { get; set; } = "test";
}
