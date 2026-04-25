using Fake.Modularity;
using Fake.Testing;

namespace Fake.Validation.FluentValidation.Tests;

[DependsOn(typeof(FakeTestingModule))]
[DependsOn(typeof(FakeValidationFluentValidationModule))]
public class FakeValidationFluentValidationTestModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        Configure<FakeValidationOptions>(options =>
        {
            // 如果不移除的话 fluent validation和data annotation都会校验一波
            options.Contributors.Remove<DataAnnotationObjectValidationContributor>();
        });
    }
}