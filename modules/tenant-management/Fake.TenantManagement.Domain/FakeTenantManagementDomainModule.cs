using Fake.DomainDrivenDesign;
using Fake.Localization;
using Fake.Modularity;
using Fake.TenantManagement.Domain.Localization;
using Fake.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.TenantManagement.Domain;

[DependsOn(typeof(FakeDddDomainModule))]
public class FakeTenantManagementDomainModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddFakeVirtualFileSystem<FakeTenantManagementDomainModule>();

        context.Services.Configure<FakeLocalizationOptions>(options =>
        {
            options.Resources.Add<FakeTenantManagementDomainResource>("zh")
                .AddVirtualJson("/Fake/TenantManagement/Domain/Localization/Resources");
            options.MapErrorCodeNamespace("Fake.TenantManagement.Domain", typeof(FakeTenantManagementDomainResource));
        });
    }
}