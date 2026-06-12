using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Fake.Json.SystemTextJson;

public class FakeSystemTextJsonSerializer(IOptions<JsonSerializerOptions> options) : IFakeJsonSerializer
{
    private static readonly ConcurrentDictionary<object, JsonSerializerOptions> OptionsCache = new();

    public string Serialize(object obj, bool camelCase = true, bool indented = false)
    {
        return JsonSerializer.Serialize(obj, GetJsonSerializerOptions(camelCase, indented));
    }

    public T? Deserialize<T>(string jsonString, bool camelCase = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(jsonString, GetJsonSerializerOptions(camelCase));
        }
        catch
        {
            return default;
        }
    }

    public object? Deserialize(string jsonString, Type type, bool camelCase = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return default;
            }

            return JsonSerializer.Deserialize(jsonString, type, GetJsonSerializerOptions(camelCase));
        }
        catch
        {
            return default;
        }
    }


    protected virtual JsonSerializerOptions GetJsonSerializerOptions(bool camelCase = true, bool indented = false)
    {
        return OptionsCache.GetOrAdd(new
        {
            camelCase, indented, options.Value
        }, _ =>
        {
            var settings = new JsonSerializerOptions(options.Value);

            if (camelCase)
            {
                settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }

            if (indented)
            {
                settings.WriteIndented = true;
            }

            return settings;
        });
    }
}