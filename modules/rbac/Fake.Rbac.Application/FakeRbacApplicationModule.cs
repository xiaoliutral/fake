using Fake.AspNetCore;
using Fake.Modularity;
using Fake.ObjectMapping.AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.Rbac.Application;

[DependsOn(
    typeof(FakeAspNetCoreModule),
    typeof(FakeRbacDomainModule),
    typeof(FakeObjectMappingAutoMapperModule)
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