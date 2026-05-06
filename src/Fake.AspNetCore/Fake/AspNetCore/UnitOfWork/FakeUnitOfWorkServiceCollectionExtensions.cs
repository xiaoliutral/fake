using Fake.Modularity;
using Fake.UnitOfWork;

namespace Fake.AspNetCore.UnitOfWork;

public static class FakeUnitOfWorkServiceCollectionExtensions
{
    /// <summary>
    /// 工作单元过滤器
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddFakeUnitOfWorkActionFilter(this IServiceCollection services)
    {
        FakeModuleHelper.EnsureDependsOn<FakeUnitOfWorkModule>();
        return services.AddFakeFilter<FakeUnitOfWorkActionFilter>();
    }
}