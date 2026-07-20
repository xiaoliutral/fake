using System.Text;
using Fake.DependencyInjection;
using FreeRedis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Fake.Caching.FreeRedis;

/// <summary>
/// FreeRedis 实现的 <see cref="IDistributedCache"/>，并支持批量操作以配合 <see cref="IFakeDistributedCache{TCacheItem}"/>。
/// 存储格式与官方 FreeRedis.DistributedCache 一致（Hash: absexp/sldexp/data）。
/// </summary>
[DisableServiceRegistration]
public class FakeFreeRedisCache : IDistributedCache, ICacheSupportsMultipleItems
{
    private const string SetScript = """
                                     redis.call('HMSET', KEYS[1], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', ARGV[4])
                                     if ARGV[3] ~= '-1' then
                                       redis.call('EXPIRE', KEYS[1], ARGV[3])
                                     end
                                     return 1
                                     """;

    private const string AbsoluteExpirationKey = "absexp";
    private const string SlidingExpirationKey = "sldexp";
    private const string DataKey = "data";
    private const long NotPresent = -1;

    private static readonly string[] HashMembersAbsoluteExpirationSlidingExpirationData =
        [AbsoluteExpirationKey, SlidingExpirationKey, DataKey];

    private static readonly string[] HashMembersAbsoluteExpirationSlidingExpiration =
        [AbsoluteExpirationKey, SlidingExpirationKey];

    protected RedisClient RedisClient { get; }

    protected string InstancePrefix { get; }

    public FakeFreeRedisCache(
        RedisClient redisClient,
        IOptions<FakeFreeRedisCacheOptions>? options = null)
    {
        RedisClient = ThrowHelper.ThrowIfNull(redisClient, nameof(redisClient));
        InstancePrefix = options?.Value.InstanceName ?? string.Empty;
    }

    protected virtual string PrefixedKey(string key) => InstancePrefix + key;

    public byte[]? Get(string key)
    {
        ThrowHelper.ThrowIfNull(key, nameof(key));
        return GetAndRefresh(PrefixedKey(key), getData: true);
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        ThrowHelper.ThrowIfNull(key, nameof(key));
        token.ThrowIfCancellationRequested();
        return await GetAndRefreshAsync(PrefixedKey(key), getData: true, token);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ThrowHelper.ThrowIfNull(key, nameof(key));
        ThrowHelper.ThrowIfNull(value, nameof(value));
        ThrowHelper.ThrowIfNull(options, nameof(options));

        SetCore(PrefixedKey(key), value, options);
    }

    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        ThrowHelper.ThrowIfNull(key, nameof(key));
        ThrowHelper.ThrowIfNull(value, nameof(value));
        ThrowHelper.ThrowIfNull(options, nameof(options));
        token.ThrowIfCancellationRequested();

        await SetCoreAsync(PrefixedKey(key), value, options);
    }

    public void Refresh(string key)
    {
        ThrowHelper.ThrowIfNull(key, nameof(key));
        GetAndRefresh(PrefixedKey(key), getData: false);
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        ThrowHelper.ThrowIfNull(key, nameof(key));
        token.ThrowIfCancellationRequested();
        await GetAndRefreshAsync(PrefixedKey(key), getData: false, token);
    }

    public void Remove(string key)
    {
        ThrowHelper.ThrowIfNull(key, nameof(key));
        RedisClient.Del(PrefixedKey(key));
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        ThrowHelper.ThrowIfNull(key, nameof(key));
        token.ThrowIfCancellationRequested();
        await RedisClient.DelAsync(PrefixedKey(key));
    }

    public byte[]?[] GetMany(IEnumerable<string> keys)
    {
        var keyArray = keys.Select(PrefixedKey).ToArray();
        var result = new byte[]?[keyArray.Length];

        for (var i = 0; i < keyArray.Length; i++)
        {
            result[i] = GetAndRefresh(keyArray[i], getData: true);
        }

        return result;
    }

    public async Task<byte[]?[]> GetManyAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var keyArray = keys.Select(PrefixedKey).ToArray();
        var result = new byte[]?[keyArray.Length];

        for (var i = 0; i < keyArray.Length; i++)
        {
            result[i] = await GetAndRefreshAsync(keyArray[i], getData: true, token);
        }

        return result;
    }

    public void SetMany(IEnumerable<KeyValuePair<string, byte[]>> items, DistributedCacheEntryOptions options)
    {
        ThrowHelper.ThrowIfNull(options, nameof(options));

        foreach (var item in items)
        {
            SetCore(PrefixedKey(item.Key), item.Value, options);
        }
    }

    public async Task SetManyAsync(
        IEnumerable<KeyValuePair<string, byte[]>> items,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        ThrowHelper.ThrowIfNull(options, nameof(options));
        token.ThrowIfCancellationRequested();

        foreach (var item in items)
        {
            await SetCoreAsync(PrefixedKey(item.Key), item.Value, options);
        }
    }

    public void RefreshMany(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            GetAndRefresh(PrefixedKey(key), getData: false);
        }
    }

    public async Task RefreshManyAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        foreach (var key in keys)
        {
            await GetAndRefreshAsync(PrefixedKey(key), getData: false, token);
        }
    }

    public void RemoveMany(IEnumerable<string> keys)
    {
        var keyArray = keys.Select(PrefixedKey).ToArray();
        if (keyArray.Length > 0)
        {
            RedisClient.Del(keyArray);
        }
    }

    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var keyArray = keys.Select(PrefixedKey).ToArray();
        if (keyArray.Length > 0)
        {
            await RedisClient.DelAsync(keyArray);
        }
    }

    protected virtual void SetCore(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var creationTime = DateTimeOffset.UtcNow;
        var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

        RedisClient.Eval(SetScript, [key],
        [
            absoluteExpiration?.Ticks ?? NotPresent,
            options.SlidingExpiration?.Ticks ?? NotPresent,
            GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
            value
        ]);
    }

    protected virtual async Task SetCoreAsync(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var creationTime = DateTimeOffset.UtcNow;
        var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

        await RedisClient.EvalAsync(SetScript, [key],
        [
            absoluteExpiration?.Ticks ?? NotPresent,
            options.SlidingExpiration?.Ticks ?? NotPresent,
            GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
            value
        ]);
    }

    protected virtual byte[]? GetAndRefresh(string key, bool getData)
    {
        var results = RedisClient.HMGet<byte[]>(key, GetHashFields(getData));

        if (results.Length >= 2)
        {
            MapMetadata(results, out var absExpr, out var sldExpr);
            RefreshExpiration(key, absExpr, sldExpr);
        }

        return results.Length >= 3 ? results[2] : null;
    }

    protected virtual async Task<byte[]?> GetAndRefreshAsync(string key, bool getData, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var results = await RedisClient.HMGetAsync<byte[]>(key, GetHashFields(getData));

        if (results.Length >= 2)
        {
            MapMetadata(results, out var absExpr, out var sldExpr);
            await RefreshExpirationAsync(key, absExpr, sldExpr);
        }

        return results.Length >= 3 ? results[2] : null;
    }

    protected virtual void RefreshExpiration(string key, DateTimeOffset? absExpr, TimeSpan? sldExpr)
    {
        if (!sldExpr.HasValue)
        {
            return;
        }

        TimeSpan? expr;
        if (absExpr.HasValue)
        {
            var relExpr = absExpr.Value - DateTimeOffset.Now;
            expr = relExpr <= sldExpr.Value ? relExpr : sldExpr;
        }
        else
        {
            expr = sldExpr;
        }

        RedisClient.Expire(key, expr ?? TimeSpan.Zero);
    }

    protected virtual async Task RefreshExpirationAsync(string key, DateTimeOffset? absExpr, TimeSpan? sldExpr)
    {
        if (!sldExpr.HasValue)
        {
            return;
        }

        TimeSpan? expr;
        if (absExpr.HasValue)
        {
            var relExpr = absExpr.Value - DateTimeOffset.Now;
            expr = relExpr <= sldExpr.Value ? relExpr : sldExpr;
        }
        else
        {
            expr = sldExpr;
        }

        await RedisClient.ExpireAsync(key, (int)(expr ?? TimeSpan.Zero).TotalSeconds);
    }

    protected static void MapMetadata(
        byte[][] results,
        out DateTimeOffset? absoluteExpiration,
        out TimeSpan? slidingExpiration)
    {
        absoluteExpiration = null;
        slidingExpiration = null;

        var absoluteExpirationStr = results[0] == null ? null : Encoding.UTF8.GetString(results[0]);
        if (long.TryParse(absoluteExpirationStr, out var absoluteExpirationTicks) &&
            absoluteExpirationTicks != NotPresent)
        {
            absoluteExpiration = new DateTimeOffset(absoluteExpirationTicks, TimeSpan.Zero);
        }

        var slidingExpirationStr = results[1] == null ? null : Encoding.UTF8.GetString(results[1]);
        if (long.TryParse(slidingExpirationStr, out var slidingExpirationTicks) &&
            slidingExpirationTicks != NotPresent)
        {
            slidingExpiration = new TimeSpan(slidingExpirationTicks);
        }
    }

    protected static long? GetExpirationInSeconds(
        DateTimeOffset creationTime,
        DateTimeOffset? absoluteExpiration,
        DistributedCacheEntryOptions options)
    {
        if (absoluteExpiration.HasValue && options.SlidingExpiration.HasValue)
        {
            return (long)Math.Min(
                (absoluteExpiration.Value - creationTime).TotalSeconds,
                options.SlidingExpiration.Value.TotalSeconds);
        }

        if (absoluteExpiration.HasValue)
        {
            return (long)(absoluteExpiration.Value - creationTime).TotalSeconds;
        }

        if (options.SlidingExpiration.HasValue)
        {
            return (long)options.SlidingExpiration.Value.TotalSeconds;
        }

        return null;
    }

    protected static DateTimeOffset? GetAbsoluteExpiration(
        DateTimeOffset creationTime,
        DistributedCacheEntryOptions options)
    {
        if (options.AbsoluteExpiration.HasValue && options.AbsoluteExpiration <= creationTime)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
                options.AbsoluteExpiration.Value,
                "The absolute expiration value must be in the future.");
        }

        var absoluteExpiration = options.AbsoluteExpiration;
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            absoluteExpiration = creationTime + options.AbsoluteExpirationRelativeToNow;
        }

        return absoluteExpiration;
    }

    private static string[] GetHashFields(bool getData)
    {
        return getData
            ? HashMembersAbsoluteExpirationSlidingExpirationData
            : HashMembersAbsoluteExpirationSlidingExpiration;
    }
}
