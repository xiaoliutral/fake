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
    /// 签名 URL 默认有效期（秒）。仅当请求签名访问时使用。
    /// </summary>
    public int DefaultSignedUrlExpiresSeconds { get; set; } = 3600;

    public Dictionary<string, FileCategoryOptions> Categories { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public class FileCategoryOptions
{
    public long? MaxSizeBytes { get; set; }

    public string[]? AllowedExtensions { get; set; }

    /// <summary>
    /// 是否默认使用签名 URL。
    /// </summary>
    public bool UseSignedUrl { get; set; }
}
