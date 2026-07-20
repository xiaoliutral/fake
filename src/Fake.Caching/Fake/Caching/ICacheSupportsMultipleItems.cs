using Microsoft.Extensions.Caching.Distributed;

namespace Fake.Caching;

/// <summary>
/// 底层字节缓存是否支持批量操作（如 Redis 管线）
/// </summary>
public interface ICacheSupportsMultipleItems
{
    byte[]?[] GetMany(IEnumerable<string> keys);

    Task<byte[]?[]> GetManyAsync(IEnumerable<string> keys, CancellationToken token = default);

    void SetMany(IEnumerable<KeyValuePair<string, byte[]>> items, DistributedCacheEntryOptions options);

    Task SetManyAsync(
        IEnumerable<KeyValuePair<string, byte[]>> items,
        DistributedCacheEntryOptions options,
        CancellationToken token = default);

    void RefreshMany(IEnumerable<string> keys);

    Task RefreshManyAsync(IEnumerable<string> keys, CancellationToken token = default);

    void RemoveMany(IEnumerable<string> keys);

    Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken token = default);
}
