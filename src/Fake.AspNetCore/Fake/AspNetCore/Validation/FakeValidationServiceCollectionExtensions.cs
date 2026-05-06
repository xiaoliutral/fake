using Fake.Modularity;
using Fake.Validation;

namespace Fake.AspNetCore.Validation;

public static class FakeValidationServiceCollectionExtensions
{
    /// <summary>
    /// 入参校验过滤器
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddFakeValidationActionFilter(this IServiceCollection services)
    {
        FakeModuleHelper.EnsureDependsOn<FakeValidationModule>();
        services.AddTransient<IModelStateValidator, ModelStateValidator>();
        return services.AddFakeFilter<FakeValidationActionFilter>();
    }
}