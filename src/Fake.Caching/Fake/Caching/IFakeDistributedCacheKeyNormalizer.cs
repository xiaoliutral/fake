namespace Fake.Caching;

public interface IFakeDistributedCacheKeyNormalizer
{
    string NormalizeKey(FakeDistributedCacheKeyNormalizeArgs args);
}
