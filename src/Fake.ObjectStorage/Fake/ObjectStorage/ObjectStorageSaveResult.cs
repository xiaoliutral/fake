namespace Fake.ObjectStorage;

public class ObjectStorageSaveResult
{
    public required string ObjectKey { get; init; }

    /// <summary>
    /// 立即可用的访问地址。
    /// </summary>
    public required string Url { get; init; }

    public long? Size { get; init; }
}
