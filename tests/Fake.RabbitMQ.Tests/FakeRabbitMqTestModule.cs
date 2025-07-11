using Fake.Autofac;
using Fake.Modularity;
using Fake.RabbitMQ;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;

[DependsOn(typeof(FakeAutofacModule))]
[DependsOn(typeof(FakeRabbitMqModule))]
[DependsOn(typeof(FakeTestingModule))]
public class FakeRabbitMqTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}