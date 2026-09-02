using System.Text;
using Fake.Json;
using Fake.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Fake.Caching;

/// <summary>
/// 分布式缓存实现。值类型由方法泛型参数指定。
/// </summary>
public class FakeDistributedCache : IFakeDistributedCache
{
    private readonly FakeDistributedCacheOptions _options;
    private readonly IDistributedCache _cache;
    private readonly ICancellationTokenProvider _cancellationTokenProvider;
    private readonly IFakeJsonSerializer _jsonSerializer;
    private readonly SemaphoreSlim _syncSemaphore;

    public FakeDistributedCache(
        IOptions<FakeDistributedCacheOptions> options,
        IDistributedCache cache,
        ICancellationTokenProvider cancellationTokenProvider,
        IFakeJsonSerializer jsonSerializer)
    {
        _options = options.Value;
        _cache = cache;
        _cancellationTokenProvider = cancellationTokenProvider;
        _jsonSerializer = jsonSerializer;
        _syncSemaphore = new SemaphoreSlim(1, 1);
    }

    public virtual async Task<TCacheItem?> GetAsync<TCacheItem>(
        string key,
        CancellationToken token = default)
        where TCacheItem : class
    {
        var cachedBytes = await _cache.GetAsync(
            NormalizeKey(key),
            _cancellationTokenProvider.FallbackToProvider(token));

        return ToCacheItem<TCacheItem>(cachedBytes);
    }

    public virtual async Task<KeyValuePair<string, TCacheItem?>[]> GetManyAsync<TCacheItem>(
        IEnumerable<string> keys,
        CancellationToken token = default)
        where TCacheItem : class
    {
        var keyArray = keys.ToArray();
        if (_cache is not ICacheSupportsMultipleItems multi)
        {
            return await GetManyFallbackAsync<TCacheItem>(keyArray, token);
        }

        var cachedBytes = await multi.GetManyAsync(
            keyArray.Select(NormalizeKey),
            _cancellationTokenProvider.FallbackToProvider(token));

        return ToCacheItems<TCacheItem>(cachedBytes, keyArray);
    }

    public virtual async Task<TCacheItem?> GetOrAddAsync<TCacheItem>(
        string key,
        Func<Task<TCacheItem>> factory,
        Func<DistributedCacheEntryOptions>? optionsFactory = null,
        CancellationToken token = default)
        where TCacheItem : class
    {
        token = _cancellationTokenProvider.FallbackToProvider(token);

        var value = await GetAsync<TCacheItem>(key, token);
        if (value != null)
        {
            return value;
        }

        using (await _syncSemaphore.BeginScopeAsync(token))
        {
            value = await GetAsync<TCacheItem>(key, token);
            if (value != null)
            {
                return value;
            }

            value = await factory();
            if (value == null)
            {
                return null;
            }

            await SetAsync(key, value, optionsFactory?.Invoke(), token);
            return value;
        }
    }

    public virtual async Task<KeyValuePair<string, TCacheItem?>[]> GetOrAddManyAsync<TCacheItem>(
        IEnumerable<string> keys,
        Func<IEnumerable<string>, Task<List<KeyValuePair<string, TCacheItem>>>> factory,
        Func<DistributedCacheEntryOptions>? optionsFactory = null,
        CancellationToken token = default)
        where TCacheItem : class
    {
        var keyArray = keys.ToArray();
        var result = await GetManyAsync<TCacheItem>(keyArray, token);
        var missingKeys = result.Where(x => x.Value == null).Select(x => x.Key).ToList();
        if (missingKeys.Count == 0)
        {
            return result;
        }

        var missingValues = await factory(missingKeys);
        var missingDict = missingValues.ToDictionary(x => x.Key, x => x.Value);

        if (missingValues.Count > 0)
        {
            await SetManyAsync(missingValues, optionsFactory?.Invoke(), token);
        }

        return keyArray
            .Select(key =>
            {
                var existing = result.First(x => x.Key == key);
                if (existing.Value != null)
                {
                    return existing;
                }

                return missingDict.TryGetValue(key, out var created)
                    ? new KeyValuePair<string, TCacheItem?>(key, created)
                    : new KeyValuePair<string, TCacheItem?>(key, null);
            })
            .ToArray();
    }

    public virtual Task SetAsync<TCacheItem>(
        string key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken token = default)
        where TCacheItem : class
    {
        return _cache.SetAsync(
            NormalizeKey(key),
            Serialize(value),
            options ?? GetDefaultCacheEntryOptions<TCacheItem>(),
            _cancellationTokenProvider.FallbackToProvider(token));
    }

    public virtual async Task SetManyAsync<TCacheItem>(
        IEnumerable<KeyValuePair<string, TCacheItem>> items,
        DistributedCacheEntryOptions? options = null,
        CancellationToken token = default)
        where TCacheItem : class
    {
        var itemArray = items.ToArray();
        if (_cache is not ICacheSupportsMultipleItems multi)
        {
            await SetManyFallbackAsync(itemArray, options, token);
            return;
        }

        await multi.SetManyAsync(
            ToRawCacheItems(itemArray),
            options ?? GetDefaultCacheEntryOptions<TCacheItem>(),
            _cancellationTokenProvider.FallbackToProvider(token));
    }

    public virtual Task RefreshAsync<TCacheItem>(
        string key,
        CancellationToken token = default)
        where TCacheItem : class
    {
        return _cache.RefreshAsync(
            NormalizeKey(key),
            _cancellationTokenProvider.FallbackToProvider(token));
    }

    public virtual async Task RefreshManyAsync<TCacheItem>(
        IEnumerable<string> keys,
        CancellationToken token = default)
        where TCacheItem : class
    {
        token = _cancellationTokenProvider.FallbackToProvider(token);
        if (_cache is ICacheSupportsMultipleItems multi)
        {
            await multi.RefreshManyAsync(keys.Select(NormalizeKey), token);
            return;
        }

        foreach (var key in keys)
        {
            await _cache.RefreshAsync(NormalizeKey(key), token);
        }
    }

    public virtual Task RemoveAsync<TCacheItem>(
        string key,
        CancellationToken token = default)
        where TCacheItem : class
    {
        return _cache.RemoveAsync(
            NormalizeKey(key),
            _cancellationTokenProvider.FallbackToProvider(token));
    }

    public virtual async Task RemoveManyAsync<TCacheItem>(
        IEnumerable<string> keys,
        CancellationToken token = default)
        where TCacheItem : class
    {
        token = _cancellationTokenProvider.FallbackToProvider(token);
        var keyArray = keys.ToArray();
        if (_cache is ICacheSupportsMultipleItems multi)
        {
            await multi.RemoveManyAsync(keyArray.Select(NormalizeKey), token);
            return;
        }

        foreach (var key in keyArray)
        {
            await _cache.RemoveAsync(NormalizeKey(key), token);
        }
    }

    private async Task<KeyValuePair<string, TCacheItem?>[]> GetManyFallbackAsync<TCacheItem>(
        string[] keys,
        CancellationToken token)
        where TCacheItem : class
    {
        var result = new List<KeyValuePair<string, TCacheItem?>>();
        foreach (var key in keys)
        {
            result.Add(new KeyValuePair<string, TCacheItem?>(
                key,
                await GetAsync<TCacheItem>(key, token)));
        }

        return result.ToArray();
    }

    private async Task SetManyFallbackAsync<TCacheItem>(
        KeyValuePair<string, TCacheItem>[] items,
        DistributedCacheEntryOptions? options,
        CancellationToken token)
        where TCacheItem : class
    {
        foreach (var item in items)
        {
            await SetAsync(item.Key, item.Value, options, token);
        }
    }

    private string NormalizeKey(string key) => _options.KeyPrefix + key;

    private DistributedCacheEntryOptions GetDefaultCacheEntryOptions<TCacheItem>() where TCacheItem : class
    {
        var cacheName = CacheNameAttribute.GetCacheName(typeof(TCacheItem));
        foreach (var configure in _options.CacheConfigurators)
        {
            var entryOptions = configure.Invoke(cacheName);
            if (entryOptions != null)
            {
                return entryOptions;
            }
        }

        return _options.GlobalCacheEntryOptions;
    }

    private KeyValuePair<string, TCacheItem?>[] ToCacheItems<TCacheItem>(byte[]?[] itemBytes, string[] itemKeys)
        where TCacheItem : class
    {
        if (itemBytes.Length != itemKeys.Length)
        {
            throw new FakeException("count of the item bytes should be same with the count of the given keys");
        }

        var result = new KeyValuePair<string, TCacheItem?>[itemKeys.Length];
        for (var i = 0; i < itemKeys.Length; i++)
        {
            result[i] = new KeyValuePair<string, TCacheItem?>(itemKeys[i], ToCacheItem<TCacheItem>(itemBytes[i]));
        }

        return result;
    }

    private TCacheItem? ToCacheItem<TCacheItem>(byte[]? bytes) where TCacheItem : class
        => bytes == null ? null : _jsonSerializer.Deserialize<TCacheItem>(Encoding.UTF8.GetString(bytes));

    private byte[] Serialize<TCacheItem>(TCacheItem value) where TCacheItem : class
        => Encoding.UTF8.GetBytes(_jsonSerializer.Serialize(value));

    private KeyValuePair<string, byte[]>[] ToRawCacheItems<TCacheItem>(KeyValuePair<string, TCacheItem>[] items)
        where TCacheItem : class
    {
        return items
            .Select(i => new KeyValuePair<string, byte[]>(
                NormalizeKey(i.Key),
                Serialize(i.Value)))
            .ToArray();
    }
}
