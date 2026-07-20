using Fake.EventBus.RabbitMQ;
using Fake.Modularity;
using Fake.Testing;

namespace Fake.EventBus.RabbitMQ.Tests;

[DependsOn(typeof(FakeTestingModule))]
[DependsOn(typeof(FakeEventBusRabbitMqModule))]
public class FakeEventBusRabbitMqTestModule : FakeModule
{
}
