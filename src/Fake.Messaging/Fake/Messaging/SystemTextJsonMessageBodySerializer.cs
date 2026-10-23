using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fake.Messaging;

public sealed class SystemTextJsonMessageBodySerializer : IMessageBodySerializer
{
    public static readonly SystemTextJsonMessageBodySerializer Instance = new();

    private readonly JsonSerializerOptions _options;

    public SystemTextJsonMessageBodySerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? CreateDefaultOptions();
    }

    public byte[] Serialize(object value, Type type)
        => JsonSerializer.SerializeToUtf8Bytes(value, type, _options);

    public object? Deserialize(ReadOnlySpan<byte> utf8Json, Type type)
        => JsonSerializer.Deserialize(utf8Json, type, _options);

    public static JsonSerializerOptions CreateDefaultOptions()
        => new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
}
