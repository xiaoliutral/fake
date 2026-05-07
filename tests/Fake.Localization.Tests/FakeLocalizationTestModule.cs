using Fake.Localization.Tests.Localization;
using Fake.Modularity;
using Fake.Testing;
using Fake.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.Localization.Tests;

[DependsOn(typeof(FakeTestingModule))]
[DependsOn(typeof(FakeLocalizationModule))]
public class FakeLocalizationTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddFakeVirtualFileSystem<FakeLocalizationTestModule>("Fake.Localization.Tests");

        context.Services.Configure<FakeLocalizationOptions>(options =>
        {
            options.DefaultResourceType = typeof(LocalizationTestResource);
            
            options.Resources.Add<LocalizationTestResource>("zh")
                .AddVirtualJson("/Localization/Resources");

            options.Resources.Add("LocalizationTestCountryNames", "zh")
                .AddVirtualJson("/Localization/Resources/CountryNames");

            options.Resources.Add<LocalizationTestValidationResource>("zh")
                .AddVirtualJson("/Localization/Resources/Validation");
        });
    }
}