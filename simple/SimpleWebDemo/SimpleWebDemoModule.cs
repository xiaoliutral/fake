using Fake.AspNetCore;
using Fake.AspNetCore.Auditing;
using Fake.AspNetCore.Mvc;
using Fake.AspNetCore.Mvc.Conventions;
using Fake.Autofac;
using Fake.Localization;
using Fake.Modularity;
using Fake.VirtualFileSystem;
using SimpleWebDemo.Localization;

[DependsOn(typeof(FakeAutofacModule))]
[DependsOn(typeof(FakeAspNetCoreModule))]
public class SimpleWebDemoModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeAspNetCoreMvcOptions>(options =>
        {
            options.ApplicationServices2Controller<SimpleWebDemoModule>();
        });
        context.Services.AddFakeSwaggerGen();
        context.Services.AddFakeAspNetCoreAuditing();

        context.Services.Configure<FakeVirtualFileSystemOptions>(options =>
        {
            options.FileProviders.Add<SimpleWebDemoModule>("SimpleWebDemo");
        });
        context.Services.Configure<FakeLocalizationOptions>(options =>
        {
            options.Resources.Add<SimpleWebDemoResource>("zh")
                .LoadVirtualJson("/Localization/Resources");
        });
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
        var app = context.GetWebApplication();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseFakeSwagger();

        app.MapControllers();
    }
}