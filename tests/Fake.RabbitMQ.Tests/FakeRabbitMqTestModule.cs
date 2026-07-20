using Fake.Modularity;
using Fake.RabbitMQ;
using Fake.Testing;

namespace Fake.RabbitMQ.Tests;

[DependsOn(typeof(FakeTestingModule))]
[DependsOn(typeof(FakeRabbitMqModule))]
public class FakeRabbitMqTestModule : FakeModule
{
}
