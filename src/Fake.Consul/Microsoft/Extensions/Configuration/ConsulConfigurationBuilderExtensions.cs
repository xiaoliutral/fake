using Consul;
using Fake;
using Fake.Consul.Configuration;

namespace Microsoft.Extensions.Configuration;

public static class ConsulConfigurationBuilderExtensions
{
    /// <summary>
    /// 加载Consul上的配置文件，根据key
    /// </summary>
    /// <returns></returns>
    public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder, string key,
        Action<ConsulConfigurationSource>? options = null)
    {
        var configuration = builder.Build();
        var consulClientConfiguration = configuration.GetSection("Consul:Client").Get<ConsulClientConfiguration>() ??
                                        new ConsulClientConfiguration();
        ThrowHelper.ThrowIfNullOrWhiteSpace(key);

        if (configuration.GetSection("UseLocalConfigs").Get<bool>()) return builder;

        var consulClient = new ConsulClient(consulClientConfiguration);
        var consulConfigSource = new ConsulConfigurationSource(consulClient, key);
        options?.Invoke(consulConfigSource);
        builder.Add(consulConfigSource);

        return builder;
    }
}