namespace Fake.ObjectStorage;

/// <summary>
/// 预签名上传参数（逻辑 ObjectKey，不含 KeyPrefix）。
/// </summary>
public class ObjectStoragePresignUploadArgs
{
    /// <summary>逻辑对象键，例如 avatar/20260912/xxx.png。</summary>
    public required string ObjectKey { get; init; }

    public string? ContentType { get; init; }

    /// <summary>签名有效期；空则用配置默认上传有效期。</summary>
    public TimeSpan? Expires { get; init; }
}

/// <summary>
/// 预签名 PUT 结果：前端对该 URL 直传，文件不经业务服务器。
/// </summary>
public class ObjectStoragePresignUploadResult
{
    /// <summary>逻辑 ObjectKey，业务/元数据表应保存此值。</summary>
    public required string ObjectKey { get; init; }

    public required string UploadUrl { get; init; }

    public string Method { get; init; } = "PUT";

    public string? ContentType { get; init; }

    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public DateTime ExpireAt { get; init; }
}

/// <summary>
/// STS 临时上传凭证参数。
/// </summary>
public class ObjectStorageStsUploadArgs
{
    /// <summary>逻辑对象键；权限将尽量收窄到该对象。</summary>
    public required string ObjectKey { get; init; }

    public TimeSpan? Expires { get; init; }
}

/// <summary>
/// STS 临时上传凭证：前端用官方 SDK 直传。
/// </summary>
public class ObjectStorageStsUploadResult
{
    /// <summary>逻辑 ObjectKey（落库用）。</summary>
    public required string ObjectKey { get; init; }

    /// <summary>物理 ObjectKey（含 KeyPrefix，SDK putObject 用）。</summary>
    public required string PhysicalObjectKey { get; init; }

    public required string Bucket { get; init; }

    public required string Region { get; init; }

    public required string TmpSecretId { get; init; }

    public required string TmpSecretKey { get; init; }

    public required string SessionToken { get; init; }

    public long StartTime { get; init; }

    public long ExpiredTime { get; init; }
}
