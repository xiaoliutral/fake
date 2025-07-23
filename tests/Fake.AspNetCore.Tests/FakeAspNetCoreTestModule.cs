using Fake.AspNetCore.Testing;
using Fake.AspNetCore.Tests.Localization;
using Fake.Autofac;
using Fake.Localization;
using Fake.Modularity;
using Fake.VirtualFileSystem;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.AspNetCore.Tests;

[DependsOn(
    typeof(FakeAutofacModule),
    typeof(FakeAspNetCoreTestingModule))]
public class FakeAspNetCoreTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeVirtualFileSystemOptions>(options =>
        {
            options.FileProviders.Add<FakeAspNetCoreTestModule>("Fake.AspNetCore.Tests");
        });
        
        context.Services.Configure<FakeLocalizationOptions>(options =>
        {
            options.Resources.Add<LocalizationTestResource>("zh")
                .LoadVirtualJson("/Localization/Resources");
        });

        context.Services.Configure<FakeLocalizationOptions>(options =>
        {
            options.DefaultErrorResourceType = typeof(LocalizationTestResource);
        });
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
        var app = context.GetApplicationBuilder();
        var environment = context.GetEnvironmentOrNull();

        app.UseStaticFiles();
    }
}