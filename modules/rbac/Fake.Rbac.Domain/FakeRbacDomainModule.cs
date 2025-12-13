using Fake.DomainDrivenDesign;
using Fake.Localization;
using Fake.Modularity;
using Fake.Rbac.Domain.Localization;
using Fake.Rbac.Domain.Permissions;
using Fake.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;

[DependsOn(typeof(FakeDomainDrivenDesignModule))]
public class FakeRbacDomainModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeVirtualFileSystemOptions>(options =>
        {
            options.FileProviders.Add<FakeRbacDomainModule>();
        });

        context.Services.Configure<FakeLocalizationOptions>(options =>
        {
            options.Resources.Add<FakeRbacResource>("zh")
                .LoadVirtualJson("/Localization/Resources");
        });

        // 注册权限定义提供器
        context.Services.AddSingleton<IPermissionDefinitionProvider, RbacPermissionDefinitionProvider>();
    }
}