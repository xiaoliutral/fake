namespace Fake.Caching.FreeRedis;

public class FakeFreeRedisCacheOptions
{
    /// <summary>
    /// 键前缀（类似 StackExchangeRedis 的 InstanceName）
    /// </summary>
    public string InstanceName { get; set; } = "";
}
