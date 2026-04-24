using Fake.Localization;
using Fake.Modularity;
using Fake.Testing;
using Fake.Validation.Localization;

namespace Fake.Validation.Tests;

[DependsOn(typeof(FakeTestingModule))]
[DependsOn(typeof(FakeValidationModule))]
public class FakeValidationTestModule: FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        Configure<FakeLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(typeof(FakeValidationResource), typeof(FakeValidationTestModule).Assembly);
        });
    }
}