using Fake.Localization;
using Fake.Modularity;
using Fake.Testing;
using Fake.Validation.Localization;
using Fake.Validation.Tests.Localization;

namespace Fake.Validation.Tests;

[DependsOn(typeof(FakeTestingModule))]
[DependsOn(typeof(FakeValidationModule))]
public class FakeValidationTestModule: FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<FakeLocalizationOptions>(options =>
        {
            options.Resources.Add<ValidationTestResource>();
        });
    }
}