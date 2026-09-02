using Microsoft.Extensions.Caching.Distributed;

namespace Fake.Caching;

public class FakeDistributedCacheOptions
{
    /// <summary>
    /// 缓存键全局前缀
    /// </summary>
    public string KeyPrefix { get; set; } = "";

    /// <summary>
    /// 全局默认过期选项
    /// </summary>
    public DistributedCacheEntryOptions GlobalCacheEntryOptions { get; set; } = new();

    /// <summary>
    /// 按缓存名配置的选项工厂列表
    /// </summary>
    public List<Func<string, DistributedCacheEntryOptions?>> CacheConfigurators { get; set; } = [];

    public void ConfigureCache<TCacheItem>(DistributedCacheEntryOptions? options)
    {
        ConfigureCache(typeof(TCacheItem), options);
    }

    public void ConfigureCache(Type cacheItemType, DistributedCacheEntryOptions? options)
    {
        ConfigureCache(CacheNameAttribute.GetCacheName(cacheItemType), options);
    }

    public void ConfigureCache(string cacheName, DistributedCacheEntryOptions? options)
    {
        CacheConfigurators.Add(name => cacheName != name ? null : options);
    }
}
