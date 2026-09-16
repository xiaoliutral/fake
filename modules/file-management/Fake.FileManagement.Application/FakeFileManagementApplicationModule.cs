using Fake.AspNetCore;
using Fake.Ddd.Application;
using Fake.FileManagement.Domain;
using Fake.Modularity;
using Fake.ObjectMapping.AutoMapper;
using Fake.ObjectStorage;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.FileManagement.Application;

[DependsOn(
    typeof(FakeAspNetCoreModule),
    typeof(FakeDddApplicationModule),
    typeof(FakeObjectMappingAutoMapperModule),
    typeof(FakeObjectStorageModule),
    typeof(FakeFileManagementDomainModule)
)]
public class FakeFileManagementApplicationModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<FakeFileManagementOptions>(
            configuration.GetSection(FakeFileManagementOptions.SectionName));

        context.Services.Configure<FakeAutoMapperOptions>(options =>
        {
            options.AddProfile<AutoMapper.FileManagementAutoMapperProfile>(validate: false);
        });
    }
}
