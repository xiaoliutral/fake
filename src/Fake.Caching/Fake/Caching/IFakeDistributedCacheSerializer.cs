namespace Fake.Caching;

public interface IFakeDistributedCacheSerializer
{
    byte[] Serialize<T>(T obj);

    T Deserialize<T>(byte[] bytes);
}
