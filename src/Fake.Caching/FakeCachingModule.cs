using Fake.Modularity;
using Fake.MultiTenant;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.Caching;

[DependsOn(
    typeof(FakeUnitOfWorkModule),
    typeof(FakeMultiTenantModule))]
public class FakeCachingModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMemoryCache();
        context.Services.AddDistributedMemoryCache();

        context.Services.AddSingleton(typeof(IFakeDistributedCache<>), typeof(FakeDistributedCache<>));
        context.Services.AddSingleton(typeof(IFakeDistributedCache<,>), typeof(FakeDistributedCache<,>));

        context.Services.Configure<FakeDistributedCacheOptions>(cacheOptions =>
        {
            cacheOptions.GlobalCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
        });
    }
}
