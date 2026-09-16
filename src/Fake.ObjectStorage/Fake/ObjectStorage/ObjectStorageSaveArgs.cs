namespace Fake.ObjectStorage;

public class ObjectStorageSaveArgs
{
    /// <summary>
    /// 对象键，可带路径前缀，例如 avatars/u1_20260912.jpg。
    /// </summary>
    public required string ObjectKey { get; init; }

    public required Stream Content { get; init; }

    public string? ContentType { get; init; }

    public bool Overwrite { get; init; } = true;

    public IDictionary<string, string>? Metadata { get; init; }
}
