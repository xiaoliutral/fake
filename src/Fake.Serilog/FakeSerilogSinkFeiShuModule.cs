using Fake.DependencyInjection;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.Serilog;

[DependsOn(typeof(FakeCoreModule))]
public class FakeSerilogModule : FakeModule
{
    public override void PostConfigureApplication(ApplicationConfigureContext context)
    {
        var accessor = context.ServiceProvider.GetRequiredService<ObjectAccessor<IServiceProvider>>();
        FeiShuRuntimeServiceProviderAccessor.SetAccessor(accessor);
    }
}
