using Fake.EventBus.Distributed;

namespace Fake.EventBus.RabbitMQ.Tests.Events;

public class SimpleIntegrationEvent : IntegrationEvent
{
    public int Num { get; set; }
}
