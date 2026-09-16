using Fake.Modularity;
using Fake.ObjectStorage;
using Fake.ObjectStorage.AliyunOss;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Fake.ObjectStorage;

[DependsOn(typeof(FakeObjectStorageModule))]
public class FakeObjectStorageAliyunOssModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<AliyunOssOptions>(
            configuration.GetSection(AliyunOssOptions.SectionName));

        var provider = configuration[$"{FakeObjectStorageOptions.SectionName}:Provider"];
        if (string.Equals(provider, "AliyunOss", StringComparison.OrdinalIgnoreCase))
        {
            context.Services.Replace(
                ServiceDescriptor.Singleton<IObjectStorage, AliyunOssObjectStorage>());
        }
    }
}
