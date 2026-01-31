using Consul;
using Fake.Consul.Configuration.Parsers;

namespace Fake.Consul.Internal;

internal static class KVPairExtensions
{
    internal static IEnumerable<KeyValuePair<string, string?>> ConvertToConfig(
        this KVPair kvPair,
        string keyToRemove,
        IConfigurationParser parser)
    {
        using Stream stream = new MemoryStream(kvPair.Value);
        return parser
            .Parse(stream)
            .Select(
                pair =>
                {
                    var key = $"{kvPair.Key.RemovePrefix(keyToRemove).TrimEnd('/').Replace('/', ':')}:{pair.Key}"
                        .Trim(':');
                    if (string.IsNullOrEmpty(key))
                    {
                        throw new InvalidKeyPairException(
                            "The key must not be null or empty. Ensure that there is at least one key under the root of the config or that the data there contains more than just a single value.");
                    }

                    return new KeyValuePair<string, string?>(key, pair.Value);
                });
    }
    
    internal static bool HasValue(this KVPair kvPair)
    {
        return kvPair.IsLeafNode() && kvPair.Value != null && kvPair.Value.Any();
    }
    
    internal static bool IsLeafNode(this KVPair kvPair)
    {
        return !kvPair.Key.EndsWith("/");
    }
}