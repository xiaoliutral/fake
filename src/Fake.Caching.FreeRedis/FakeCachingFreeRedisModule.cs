using Fake.Caching.FreeRedis;
using Fake.Modularity;
using FreeRedis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Fake.Caching;

[DependsOn(typeof(FakeCachingModule))]
public class FakeCachingFreeRedisModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IRedisInitializer, RedisInitializer>();
        context.Services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IRedisInitializer>().Initialize());
        context.Services.AddSingleton<IRedisClient>(sp => sp.GetRequiredService<RedisClient>());

        var configuration = context.Services.GetConfiguration();
        var redisEnabled = configuration["Redis:IsEnabled"];
        if (string.IsNullOrEmpty(redisEnabled) || bool.Parse(redisEnabled))
        {
            context.Services.Configure<FakeFreeRedisCacheOptions>(options =>
            {
                var instanceName = configuration["Redis:InstanceName"];
                if (!instanceName.IsNullOrEmpty())
                {
                    options.InstanceName = instanceName!;
                }
            });

            context.Services.Replace(ServiceDescriptor.Singleton<IDistributedCache, FakeFreeRedisCache>());
        }
    }

    public override void PostConfigureApplication(ApplicationConfigureContext context)
    {
        RedisHelper.SetClient(context.ServiceProvider.GetRequiredService<IRedisClient>());
    }
}
