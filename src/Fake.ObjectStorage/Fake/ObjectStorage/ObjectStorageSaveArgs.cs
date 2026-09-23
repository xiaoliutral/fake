namespace Fake.ObjectStorage;

public class ObjectStorageSaveArgs
{
    public required string ObjectKey { get; init; }

    public required Stream Content { get; init; }

    public string? ContentType { get; init; }

    public bool Overwrite { get; init; } = true;

    public IDictionary<string, string>? Metadata { get; init; }
}
