using Fake.Modularity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Fake.Caching.StackExchangeRedis;

[DependsOn(typeof(FakeCachingModule))]
public class FakeCachingStackExchangeRedisModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        var redisEnabled = configuration["Redis:IsEnabled"];
        if (string.IsNullOrEmpty(redisEnabled) || bool.Parse(redisEnabled))
        {
            context.Services.AddStackExchangeRedisCache(options =>
            {
                var redisConfiguration = configuration["Redis:Configuration"];
                if (!redisConfiguration.IsNullOrEmpty())
                {
                    options.Configuration = redisConfiguration;
                }
            });

            context.Services.Replace(ServiceDescriptor.Singleton<IDistributedCache, FakeRedisCache>());
        }
    }
}
