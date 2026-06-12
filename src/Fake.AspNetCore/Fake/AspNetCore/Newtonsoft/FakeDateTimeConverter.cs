using System.Globalization;
using Fake.Json;
using Fake.Timing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Fake.AspNetCore.Newtonsoft;

public class FakeDateTimeConverter(IFakeClock clock, IOptions<FakeJsonSerializerOptions> options) : DateTimeConverterBase
{
    private readonly FakeJsonSerializerOptions _options = options.Value;
    private static readonly DateTimeStyles DateTimeStyles = DateTimeStyles.RoundtripKind;
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
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

        if (reader.TokenType == JsonToken.Date)
        {
            return clock.Normalize(reader.Value!.To<DateTime>());
        }

        if (reader.TokenType != JsonToken.String)
        {
            throw new JsonSerializationException(
                $"Unexpected token parsing date. Expected String, got {reader.TokenType}.");
        }

        var dateText = reader.Value?.ToString();

        if (dateText.IsNullOrEmpty() && isNullableType)
        {
            return null;
        }

        if (_options.InputDateTimeFormats.Any())
        {
            foreach (var format in _options.InputDateTimeFormats)
            {
                if (DateTime.TryParseExact(dateText, format, Culture, DateTimeStyles, out var d1))
                {
                    return clock.Normalize(d1);
                }
            }
        }

        var d2 = DateTime.Parse(dateText!, Culture, DateTimeStyles);
        return clock.Normalize(d2);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value != null)
        {
            value = clock.Normalize(value!.To<DateTime>());
        }

        if (value is DateTime dateTime)
        {
            if (DateTimeStyles.HasFlag(DateTimeStyles.AdjustToUniversal) ||
                DateTimeStyles.HasFlag(DateTimeStyles.AssumeUniversal))
            {
                dateTime = dateTime.ToUniversalTime();
            }

            writer.WriteValue(clock.NormalizeAsString(dateTime));
        }
        else
        {
            throw new JsonSerializationException(
                $"Unexpected value when converting date. Expected DateTime or DateTimeOffset, got {value?.GetType()}.");
        }
    }
}