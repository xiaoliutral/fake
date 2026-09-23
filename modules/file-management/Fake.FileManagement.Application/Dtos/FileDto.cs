namespace Fake.FileManagement.Application.Dtos;

public class FileDto
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = null!;

    public string? ContentType { get; set; }

    public long Size { get; set; }

    /// <summary>访问地址；仅 Available 时返回。</summary>
    public string? Url { get; set; }
}

public class PresignUploadInput
{
    public required string FileName { get; set; }

    public string? ContentType { get; set; }

    /// <summary>可选上传策略名，对应 FileManagement:Policies。</summary>
    public string? Policy { get; set; }

    /// <summary>签名有效期（秒）；空则用 ObjectStorage 默认上传有效期。</summary>
    public int? ExpiresSeconds { get; set; }
}

public class PresignUploadDto
{
    public Guid FileId { get; set; }

    public required string UploadUrl { get; set; }

    public string Method { get; set; } = "PUT";

    public string? ContentType { get; set; }

    public IReadOnlyDictionary<string, string>? Headers { get; set; }

    public DateTime ExpireAt { get; set; }
}

public class StsUploadInput
{
    public required string FileName { get; set; }

    public string? Policy { get; set; }

    public int? ExpiresSeconds { get; set; }
}

public class StsUploadDto
{
    public Guid FileId { get; set; }

    /// <summary>SDK 直传使用的物理 ObjectKey（含 KeyPrefix）。</summary>
    public required string PhysicalObjectKey { get; set; }

    public required string Bucket { get; set; }

    public required string Region { get; set; }

    public required string TmpSecretId { get; set; }

    public required string TmpSecretKey { get; set; }

    public required string SessionToken { get; set; }

    public long StartTime { get; set; }

    public long ExpiredTime { get; set; }
}

/// <summary>
/// 前端直传完成后确认：将 Pending 转为 Available。
/// </summary>
public class ConfirmUploadInput
{
    public Guid FileId { get; set; }

    /// <summary>可选；空则沿用 Pending 时写入的文件名。</summary>
    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    public long? Size { get; set; }

    /// <summary>是否校验对象已存在于存储中。默认 true。</summary>
    public bool VerifyExists { get; set; } = true;
}
