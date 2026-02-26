using Fake.DependencyInjection;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.Serilog.Sink.FeiShu;

[DependsOn(typeof(FakeCoreModule))]
public class FakeSerilogSinkFeiShuModule : FakeModule
{
    public override void PostConfigureApplication(ApplicationConfigureContext context)
    {
        var accessor = context.ServiceProvider.GetRequiredService<ObjectAccessor<IServiceProvider>>();
        FeiShuRuntimeServiceProviderAccessor.SetAccessor(accessor);
    }
}
