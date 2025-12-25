using Fake.AspNetCore;
using Fake.AspNetCore.Mvc;
using Fake.AspNetCore.Mvc.Conventions;
using Fake.Modularity;
using Fake.ObjectMapping.AutoMapper;
using Fake.TenantManagement.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.TenantManagement.Application;

[DependsOn(typeof(FakeObjectMappingAutoMapperModule))]
[DependsOn(typeof(FakeTenantManagementDomainModule))]
public class FakeTenantManagementApplicationModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeAutoMapperOptions>(options =>
        {
            options.ScanProfiles<FakeTenantManagementApplicationModule>(true);
        });
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
    }
}