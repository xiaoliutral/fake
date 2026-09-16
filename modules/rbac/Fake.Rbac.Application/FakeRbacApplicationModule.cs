using Fake.AspNetCore;
using Fake.AspNetCore.Authentication;
using Fake.Modularity;
using Fake.ObjectMapping.AutoMapper;
using Fake.ObjectStorage;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.Rbac.Application;

[DependsOn(
    typeof(FakeAspNetCoreModule),
    typeof(FakeRbacDomainModule),
    typeof(FakeObjectMappingAutoMapperModule),
    typeof(FakeObjectStorageModule)
)]
public class FakeRbacApplicationModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeAutoMapperOptions>(options =>
        {
            options.AddProfile<AutoMapper.RbacApplicationAutoMapperProfile>(validate: false);
        });

        context.Services.AddFakeJwtAuthentication();
        context.Services.AddAuthorization();
    }
}