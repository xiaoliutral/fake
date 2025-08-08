using Fake.DomainDrivenDesign;
using Fake.Localization;
using Fake.Modularity;
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
            options.Resources.Add<FakeRbacDomainModule>("zh")
                .LoadVirtualJson("/Localization/Resources");
        });
    }
}