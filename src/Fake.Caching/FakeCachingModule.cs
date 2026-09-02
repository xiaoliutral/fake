using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.Caching;

public class FakeCachingModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMemoryCache();
        context.Services.AddDistributedMemoryCache();

        context.Services.AddSingleton<IFakeDistributedCache, FakeDistributedCache>();

        context.Services.Configure<FakeDistributedCacheOptions>(cacheOptions =>
        {
            cacheOptions.GlobalCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
        });
    }
}
