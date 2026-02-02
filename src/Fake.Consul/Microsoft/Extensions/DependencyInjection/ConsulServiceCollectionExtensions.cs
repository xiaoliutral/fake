using Consul;
using Fake;
using Fake.Consul;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConsulServiceCollectionExtensions
{
    /// <summary>
    /// 添加Consul
    /// </summary>
    /// <param name="services"></param>
    /// <param name="action"></param>
    internal static IServiceCollection AddConsul(this IServiceCollection services,
        Action<ConsulClientConfiguration>? action = null)
    {
        var configuration = services.GetConfiguration();
        services.Configure<ConsulClientConfiguration>(configuration.GetSection("Consul"));

        var consulClientOptions = configuration.Get<ConsulClientConfiguration>() ?? new ConsulClientConfiguration();
        ThrowHelper.ThrowIfNull(consulClientOptions, nameof(consulClientOptions), "Consul配置为空");
        action?.Invoke(consulClientOptions);
        services.AddSingleton<IConsulClient, ConsulClient>(_ => new ConsulClient(consulClientOptions));

        // 服务注册
        services.Configure<FakeConsulRegisterOptions>(configuration.GetSection("Consul:Register"));
        
        return services;
    }
}