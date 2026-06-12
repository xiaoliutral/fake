using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fake.Json.SystemTextJson.Converters;

public class NullableDateTimeConverter(DateTimeConverter dateTimeConverter) : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return dateTimeConverter.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        dateTimeConverter.Write(writer, value.Value, options);
    }
}