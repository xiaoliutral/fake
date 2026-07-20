using Microsoft.Extensions.Caching.Distributed;

namespace Fake.Caching;

/// <summary>
/// 类型化分布式缓存，键默认为 <see cref="string"/>。
/// </summary>
public interface IFakeDistributedCache<TCacheItem> : IFakeDistributedCache<TCacheItem, string>
    where TCacheItem : class
{
    IFakeDistributedCache<TCacheItem, string> InternalCache { get; }
}

/// <summary>
/// 类型化分布式缓存（Fake 顶级缓存抽象）。
/// </summary>
/// <typeparam name="TCacheItem">缓存项类型</typeparam>
/// <typeparam name="TCacheKey">缓存键类型</typeparam>
public interface IFakeDistributedCache<TCacheItem, TCacheKey>
    where TCacheItem : class
{
    TCacheItem? Get(
        TCacheKey key,
        bool? hideErrors = null,
        bool considerUow = false
    );

    KeyValuePair<TCacheKey, TCacheItem?>[] GetMany(
        IEnumerable<TCacheKey> keys,
        bool? hideErrors = null,
        bool considerUow = false
    );

    Task<KeyValuePair<TCacheKey, TCacheItem?>[]> GetManyAsync(
        IEnumerable<TCacheKey> keys,
        bool? hideErrors = null,
        bool considerUow = false,
        CancellationToken token = default
    );

    Task<TCacheItem?> GetAsync(
        TCacheKey key,
        bool? hideErrors = null,
        bool considerUow = false,
        CancellationToken token = default
    );

    TCacheItem? GetOrAdd(
        TCacheKey key,
        Func<TCacheItem> factory,
        Func<DistributedCacheEntryOptions>? optionsFactory = null,
        bool? hideErrors = null,
        bool considerUow = false
    );

    Task<TCacheItem?> GetOrAddAsync(
        TCacheKey key,
        Func<Task<TCacheItem>> factory,
        Func<DistributedCacheEntryOptions>? optionsFactory = null,
        bool? hideErrors = null,
        bool considerUow = false,
        CancellationToken token = default
    );

    KeyValuePair<TCacheKey, TCacheItem?>[] GetOrAddMany(
        IEnumerable<TCacheKey> keys,
        Func<IEnumerable<TCacheKey>, List<KeyValuePair<TCacheKey, TCacheItem>>> factory,
        Func<DistributedCacheEntryOptions>? optionsFactory = null,
        bool? hideErrors = null,
        bool considerUow = false
    );

    Task<KeyValuePair<TCacheKey, TCacheItem?>[]> GetOrAddManyAsync(
        IEnumerable<TCacheKey> keys,
        Func<IEnumerable<TCacheKey>, Task<List<KeyValuePair<TCacheKey, TCacheItem>>>> factory,
        Func<DistributedCacheEntryOptions>? optionsFactory = null,
        bool? hideErrors = null,
        bool considerUow = false,
        CancellationToken token = default
    );

    void Set(
        TCacheKey key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        bool? hideErrors = null,
        bool considerUow = false
    );

    Task SetAsync(
        TCacheKey key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        bool? hideErrors = null,
        bool considerUow = false,
        CancellationToken token = default
    );

    void SetMany(
        IEnumerable<KeyValuePair<TCacheKey, TCacheItem>> items,
        DistributedCacheEntryOptions? options = null,
        bool? hideErrors = null,
        bool considerUow = false
    );

    Task SetManyAsync(
        IEnumerable<KeyValuePair<TCacheKey, TCacheItem>> items,
        DistributedCacheEntryOptions? options = null,
        bool? hideErrors = null,
        bool considerUow = false,
        CancellationToken token = default
    );

    void Refresh(
        TCacheKey key,
        bool? hideErrors = null
    );

    Task RefreshAsync(
        TCacheKey key,
        bool? hideErrors = null,
        CancellationToken token = default
    );

    void RefreshMany(
        IEnumerable<TCacheKey> keys,
        bool? hideErrors = null);

    Task RefreshManyAsync(
        IEnumerable<TCacheKey> keys,
        bool? hideErrors = null,
        CancellationToken token = default);

    void Remove(
        TCacheKey key,
        bool? hideErrors = null,
        bool considerUow = false
    );

    Task RemoveAsync(
        TCacheKey key,
        bool? hideErrors = null,
        bool considerUow = false,
        CancellationToken token = default
    );

    void RemoveMany(
        IEnumerable<TCacheKey> keys,
        bool? hideErrors = null,
        bool considerUow = false
    );

    Task RemoveManyAsync(
        IEnumerable<TCacheKey> keys,
        bool? hideErrors = null,
        bool considerUow = false,
        CancellationToken token = default
    );
}
