using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.ObjectStorage;

public class FakeObjectStorageModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<FakeObjectStorageOptions>(
            configuration.GetSection(FakeObjectStorageOptions.SectionName));

        context.Services.AddSingleton<IObjectStorage, LocalObjectStorage>();
    }
}
