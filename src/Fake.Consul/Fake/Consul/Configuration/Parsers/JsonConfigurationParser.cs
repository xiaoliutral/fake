using Microsoft.Extensions.Configuration.Json;

namespace Fake.Consul.Configuration.Parsers;

public class JsonConfigurationParser: IConfigurationParser
{
    public IDictionary<string, string?> Parse(Stream stream)
    {
        return JsonStreamParser.Parse(stream);
    }
    
    private sealed class JsonStreamParser : JsonStreamConfigurationProvider
    {
        private JsonStreamParser(JsonStreamConfigurationSource source)
            : base(source)
        {
        }

        public static IDictionary<string, string?> Parse(Stream stream)
        {
            var provider = new JsonStreamParser(new JsonStreamConfigurationSource { Stream = stream });
            provider.Load();
            return provider.Data!;
        }
    }
}