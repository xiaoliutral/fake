using Fake.DynamicProxy;
using Fake.Localization;
using Fake.Modularity;
using Fake.Validation.Localization;
using Fake.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.Validation;

[DependsOn(typeof(FakeLocalizationModule))]
public class FakeValidationModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var contributorTypes = new List<Type>();
        
        context.Services.OnRegistered(registrationContext =>
        {
            if (ShouldIntercept(registrationContext.ImplementationType))
            {
                registrationContext.Interceptors.TryAdd<ValidationInterceptor>();
            }
            
            if (registrationContext.ImplementationType.IsAssignableTo<IObjectValidationContributor>())
            {
                contributorTypes.Add(registrationContext.ImplementationType);
            }
        });
        
        context.Services.Configure<FakeValidationOptions>(options =>
        {
            options.Contributors.TryAdd(contributorTypes);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<ValidationInterceptor>();
        context.Services.AddTransient<IObjectValidator, ObjectValidator>();
        context.Services.AddTransient<DataAnnotationObjectValidationContributor>();
        context.Services.AddTransient<IObjectValidationContributor, DataAnnotationObjectValidationContributor>();
        context.Services.AddTransient<IAttributeValidationResultProvider, DefaultAttributeValidationResultProvider>();
        
        context.Services.AddFakeVirtualFileSystem<FakeValidationModule>();

        Configure<FakeLocalizationOptions>(options =>
        {
            options.Resources
                .Add<FakeValidationResource>("zh")
                .AddVirtualJson("/Fake/Validation/Localization/Resources");
        });
    }


    private static bool ShouldIntercept(Type type)
    {
        return !DynamicProxyIgnoreTypes.Contains(type) && type.IsAssignableTo<IValidationEnabled>();
    }
}