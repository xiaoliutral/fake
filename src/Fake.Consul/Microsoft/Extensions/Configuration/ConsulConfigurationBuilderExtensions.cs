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
        ThrowHelper.ThrowIfNullOrWhiteSpace(key);
        var configuration = builder.Sources.Any() ? (ConfigurationManager)builder : builder.Build();

        if (configuration.GetSection("UseLocalConfigs").Get<bool>()) return builder;

        var consulClientConfiguration = configuration.GetSection("Consul").Get<ConsulClientConfiguration>() ??
                                        throw new ArgumentException("Consul is must configured");

        var consulClient = new ConsulClient(consulClientConfiguration);
        var consulConfigSource = new ConsulConfigurationSource(consulClient, key);
        options?.Invoke(consulConfigSource);

        builder.Add(consulConfigSource);

        builder.Build();
        return builder;
    }
}