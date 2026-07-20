namespace Fake.Caching;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
public class CacheNameAttribute(string name) : Attribute
{
    public string Name { get; } = ThrowHelper.ThrowIfNull(name, nameof(name));

    public static string GetCacheName<TCacheItem>()
    {
        return GetCacheName(typeof(TCacheItem));
    }

    public static string GetCacheName(Type cacheItemType)
    {
        var cacheNameAttribute = cacheItemType
            .GetCustomAttributes(true)
            .OfType<CacheNameAttribute>()
            .FirstOrDefault();

        if (cacheNameAttribute != null)
        {
            return cacheNameAttribute.Name;
        }

        var fullName = cacheItemType.FullName!;
        const string postFix = "CacheItem";
        return fullName.EndsWith(postFix, StringComparison.Ordinal)
            ? fullName[..^postFix.Length]
            : fullName;
    }
}
