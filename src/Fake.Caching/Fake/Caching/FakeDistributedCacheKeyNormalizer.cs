using Fake.DependencyInjection;
using Fake.MultiTenant;
using Microsoft.Extensions.Options;

namespace Fake.Caching;

public class FakeDistributedCacheKeyNormalizer(
    ICurrentTenant currentTenant,
    IOptions<FakeDistributedCacheOptions> distributedCacheOptions)
    : IFakeDistributedCacheKeyNormalizer, ITransientDependency
{
    protected ICurrentTenant CurrentTenant { get; } = currentTenant;

    protected FakeDistributedCacheOptions DistributedCacheOptions { get; } = distributedCacheOptions.Value;

    public virtual string NormalizeKey(FakeDistributedCacheKeyNormalizeArgs args)
    {
        var normalizedKey = $"c:{args.CacheName},k:{DistributedCacheOptions.KeyPrefix}{args.Key}";

        if (!args.IgnoreMultiTenancy && CurrentTenant.Id.HasValue)
        {
            normalizedKey = $"t:{CurrentTenant.Id.Value},{normalizedKey}";
        }

        return normalizedKey;
    }
}
