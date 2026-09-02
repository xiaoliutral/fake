using Microsoft.Extensions.Caching.Distributed;

namespace Fake.Caching;

/// <summary>
/// Fake 顶级分布式缓存抽象。键为 <see cref="string"/>，值类型由方法泛型参数指定。
/// </summary>
public interface IFakeDistributedCache
{
    Task<TCacheItem?> GetAsync<TCacheItem>(
        string key,
        CancellationToken token = default
    ) where TCacheItem : class;

    Task<KeyValuePair<string, TCacheItem?>[]> GetManyAsync<TCacheItem>(
        IEnumerable<string> keys,
        CancellationToken token = default
    ) where TCacheItem : class;

    Task<TCacheItem?> GetOrAddAsync<TCacheItem>(
        string key,
        Func<Task<TCacheItem>> factory,
        Func<DistributedCacheEntryOptions>? optionsFactory = null,
        CancellationToken token = default
    ) where TCacheItem : class;

    Task<KeyValuePair<string, TCacheItem?>[]> GetOrAddManyAsync<TCacheItem>(
        IEnumerable<string> keys,
        Func<IEnumerable<string>, Task<List<KeyValuePair<string, TCacheItem>>>> factory,
        Func<DistributedCacheEntryOptions>? optionsFactory = null,
        CancellationToken token = default
    ) where TCacheItem : class;

    Task SetAsync<TCacheItem>(
        string key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken token = default
    ) where TCacheItem : class;

    Task SetManyAsync<TCacheItem>(
        IEnumerable<KeyValuePair<string, TCacheItem>> items,
        DistributedCacheEntryOptions? options = null,
        CancellationToken token = default
    ) where TCacheItem : class;

    Task RefreshAsync<TCacheItem>(
        string key,
        CancellationToken token = default
    ) where TCacheItem : class;

    Task RefreshManyAsync<TCacheItem>(
        IEnumerable<string> keys,
        CancellationToken token = default
    ) where TCacheItem : class;

    Task RemoveAsync<TCacheItem>(
        string key,
        CancellationToken token = default
    ) where TCacheItem : class;

    Task RemoveManyAsync<TCacheItem>(
        IEnumerable<string> keys,
        CancellationToken token = default
    ) where TCacheItem : class;
}
