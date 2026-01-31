namespace Fake.Consul.Configuration.Parsers;

public interface IConfigurationParser
{
    /// <summary>
    ///     解析 <see cref="Stream" />
    /// </summary>
    /// <param name="stream">The stream to parse.</param>
    /// <returns>A dictionary representing the configuration in a flattened form.</returns>
    IDictionary<string, string?> Parse(Stream stream);
}