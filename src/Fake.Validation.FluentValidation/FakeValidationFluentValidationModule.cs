using Fake.Validation.FluentValidation.Fake.Validation.FluentValidation;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
using Fake.Modularity;

namespace Fake.Validation.FluentValidation;

[DependsOn(typeof(FakeValidationModule))]
public class FakeValidationFluentValidationModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddServiceRegistrar(new FluentValidationConventionalRegistrar());
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<FluentObjectValidationContributor>();
        context.Services.AddTransient<IObjectValidationContributor, FluentObjectValidationContributor>();
    }
}