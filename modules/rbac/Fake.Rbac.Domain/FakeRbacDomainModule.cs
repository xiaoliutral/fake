using Fake.DomainDrivenDesign;
using Fake.Localization;
using Fake.Modularity;
using Fake.Rbac.Domain.Localization;
using Fake.Rbac.Domain.Permissions;
using Fake.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;

[DependsOn(typeof(FakeDddDomainModule))]
public class FakeRbacDomainModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 注册权限定义提供器
        context.Services.AddSingleton<IPermissionDefinitionProvider, RbacPermissionDefinitionProvider>();

        context.Services.AddFakeVirtualFileSystem<FakeRbacDomainModule>();
        
        Configure<FakeLocalizationOptions>(options =>
        {
            options.Resources.Add<FakeRbacDomainResource>()
                .AddVirtualJson("/Fake/Rbac/Domain/Localization/Resources");
            options.MapErrorCodeNamespace("Fake.Rbac.Domain", typeof(FakeRbacDomainResource));
        });
    }
}