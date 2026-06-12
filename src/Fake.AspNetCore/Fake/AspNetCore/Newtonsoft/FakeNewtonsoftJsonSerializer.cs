using System.Collections.Concurrent;
using Fake.DependencyInjection;
using Fake.Json;
using Newtonsoft.Json;

namespace Fake.AspNetCore.Newtonsoft;

public class FakeNewtonsoftJsonSerializer(IOptions<JsonSerializerSettings> options, FakeDateTimeConverter dateTimeConverter): IFakeJsonSerializer, ITransientDependency
{
    private static readonly ConcurrentDictionary<object, JsonSerializerSettings> OptionsCache = new();

    public string Serialize(object obj, bool camelCase = true, bool indented = false)
    {
        var settings = GetJsonSerializerOptions(camelCase, indented);
        return JsonConvert.SerializeObject(obj, settings);
    }

    public T? Deserialize<T>(string jsonString, bool camelCase = true)
    {
        var settings = GetJsonSerializerOptions(camelCase);
        return JsonConvert.DeserializeObject<T>(jsonString, settings);
    }

    public object? Deserialize(string jsonString, Type type, bool camelCase = true)
    {
        var settings = GetJsonSerializerOptions(camelCase);
        return JsonConvert.DeserializeObject(jsonString, type, settings);
    }
    
    protected virtual JsonSerializerSettings GetJsonSerializerOptions(bool camelCase = true, bool indented = false)
    {
        return OptionsCache.GetOrAdd(new
        {
            camelCase, indented, options.Value
        }, _ =>
        {
            var settings = new JsonSerializerSettings(options.Value);

            if (!camelCase)
            {
                // 默认是驼峰
                settings.ContractResolver = new FakeDefaultContractResolver(dateTimeConverter);
            }

            if (indented)
            {
                settings.Formatting = Formatting.Indented;
            }

            return settings;
        });
    }
}