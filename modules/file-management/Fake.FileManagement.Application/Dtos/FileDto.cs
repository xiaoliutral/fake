namespace Fake.FileManagement.Application.Dtos;

public class FileDto
{
    public Guid Id { get; set; }

    public string ObjectKey { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string? ContentType { get; set; }

    public long Size { get; set; }

    public string Category { get; set; } = null!;

    public string? BizType { get; set; }

    public string? BizId { get; set; }

    public string? Url { get; set; }

    public DateTime CreateTime { get; set; }
}

public class PresignUploadInput
{
    /// <summary>业务分类，例如 avatar / waybill / document。</summary>
    public required string Category { get; set; }

    public required string FileName { get; set; }

    public string? ContentType { get; set; }

    /// <summary>签名有效期（秒）；空则用 ObjectStorage 默认上传有效期。</summary>
    public int? ExpiresSeconds { get; set; }
}

public class PresignUploadDto
{
    /// <summary>逻辑 ObjectKey；Confirm 时原样回传。</summary>
    public required string ObjectKey { get; set; }

    public required string UploadUrl { get; set; }

    public string Method { get; set; } = "PUT";

    public string? ContentType { get; set; }

    public IReadOnlyDictionary<string, string> Headers { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public DateTime ExpireAt { get; set; }
}

public class StsUploadInput
{
    public required string Category { get; set; }

    public required string FileName { get; set; }

    public int? ExpiresSeconds { get; set; }
}

public class StsUploadDto
{
    public required string ObjectKey { get; set; }

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
/// 前端直传完成后，仅登记元数据（不接收文件流）。
/// </summary>
public class ConfirmUploadInput
{
    public required string ObjectKey { get; set; }

    public required string FileName { get; set; }

    public required string Category { get; set; }

    public string? ContentType { get; set; }

    public long? Size { get; set; }

    public string? BizType { get; set; }

    public string? BizId { get; set; }

    /// <summary>是否校验对象已存在于存储中。默认 true。</summary>
    public bool VerifyExists { get; set; } = true;
}
