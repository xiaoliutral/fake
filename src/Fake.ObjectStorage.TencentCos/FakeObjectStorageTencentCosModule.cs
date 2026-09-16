using Fake.Modularity;
using Fake.ObjectStorage;
using Fake.ObjectStorage.TencentCos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Fake.ObjectStorage;

[DependsOn(typeof(FakeObjectStorageModule))]
public class FakeObjectStorageTencentCosModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<TencentCosOptions>(
            configuration.GetSection(TencentCosOptions.SectionName));

        var provider = configuration[$"{FakeObjectStorageOptions.SectionName}:Provider"];
        if (string.Equals(provider, "TencentCos", StringComparison.OrdinalIgnoreCase))
        {
            context.Services.Replace(
                ServiceDescriptor.Singleton<IObjectStorage, TencentCosObjectStorage>());
        }
    }
}
