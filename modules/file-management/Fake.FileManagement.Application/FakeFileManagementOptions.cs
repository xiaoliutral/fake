namespace Fake.FileManagement.Application;

public class FakeFileManagementOptions
{
    public const string SectionName = "FileManagement";

    /// <summary>
    /// 默认单文件大小上限（字节）。
    /// </summary>
    public long DefaultMaxSizeBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>
    /// 默认允许的扩展名。
    /// </summary>
    public string[] DefaultAllowedExtensions { get; set; } =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx",
        ".zip", ".rar", ".7z"
    ];

    /// <summary>
    /// 签名 URL 默认有效期（秒）。
    /// </summary>
    public int DefaultSignedUrlExpiresSeconds { get; set; } = 3600;

    /// <summary>
    /// GetAccessUrl 未显式传 expires 时，是否默认签发临时读签名。
    /// </summary>
    public bool SignUrlsByDefault { get; set; }

    /// <summary>
    /// 默认存储源名称（一期单源）。
    /// </summary>
    public string DefaultStorageSource { get; set; } = "default";

    /// <summary>
    /// 可选：按策略名覆盖大小/扩展名校验（不落库，仅上传时传入 policy 使用）。
    /// </summary>
    public Dictionary<string, FileUploadPolicyOptions> Policies { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public class FileUploadPolicyOptions
{
    public long? MaxSizeBytes { get; set; }

    public string[]? AllowedExtensions { get; set; }
}
