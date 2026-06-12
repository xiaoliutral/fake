using Fake.Json;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Fake.AspNetCore.Newtonsoft;

public class FakeLongConverter(IOptions<FakeJsonSerializerOptions> options) : JsonConverter
{
    private readonly FakeJsonSerializerOptions _options = options.Value;

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(long) || objectType == typeof(long?);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var isNullableType = Nullable.GetUnderlyingType(objectType) != null;
        if (reader.TokenType == JsonToken.Null)
        {
            if (!isNullableType)
            {
                throw new JsonSerializationException($"Cannot convert null value to {objectType.FullName}.");
            }

            return null;
        }

        if (reader.TokenType == JsonToken.Integer)
        {
            return reader.Value!.To<long>();
        }

        if (reader.TokenType != JsonToken.String)
        {
            throw new JsonSerializationException(
                $"Unexpected token parsing long. Expected String, got {reader.TokenType}.");
        }

        var longText = reader.Value?.ToString();

        return long.TryParse(longText, out var longValue) ? longValue : null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (_options.LongToString)
        {
            writer.WriteValue(value?.ToString());
        }
        else
        {
            writer.WriteValue(value);
        }
    }
}