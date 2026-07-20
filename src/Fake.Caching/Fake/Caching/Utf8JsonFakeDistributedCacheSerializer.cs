using System.Text;
using Fake.DependencyInjection;
using Fake.Json;

namespace Fake.Caching;

public class Utf8JsonFakeDistributedCacheSerializer(IFakeJsonSerializer jsonSerializer)
    : IFakeDistributedCacheSerializer, ITransientDependency
{
    protected IFakeJsonSerializer JsonSerializer { get; } = jsonSerializer;

    public byte[] Serialize<T>(T obj)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj!));
    }

    public T Deserialize<T>(byte[] bytes)
    {
        return (T)JsonSerializer.Deserialize(Encoding.UTF8.GetString(bytes), typeof(T))!;
    }
}
