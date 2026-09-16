namespace Fake.ObjectStorage;

public class FakeObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    /// <summary>
    /// Provider 名称，与具体实现包约定一致（例如 Local / AliyunOss / TencentCos）。默认 Local。
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// 全局对象键前缀，用于环境隔离（例如 dev / staging / prod）。
    /// 业务传入逻辑 key（avatars/a.jpg），实际存储为 {KeyPrefix}/avatars/a.jpg。
    /// </summary>
    public string? KeyPrefix { get; set; }

    public LocalObjectStorageOptions Local { get; set; } = new();

    /// <summary>
    /// 可选的全局大小限制（字节）。未配置则不校验。
    /// </summary>
    public long? MaxSizeBytes { get; set; }

    /// <summary>
    /// 可选的全局 ContentType 白名单。未配置则不校验。
    /// </summary>
    public string[]? AllowedContentTypes { get; set; }

    /// <summary>
    /// 私有桶场景：GetUrlAsync / Save 返回 URL 时默认签发临时读签名。
    /// Local 忽略该选项。
    /// </summary>
    public bool SignUrlsByDefault { get; set; }

    /// <summary>
    /// 默认读签名有效期（秒），配合 <see cref="SignUrlsByDefault"/> 使用。
    /// </summary>
    public int DefaultSignedUrlExpiresSeconds { get; set; } = 3600;

    /// <summary>
    /// 默认上传凭证（预签名 PUT / STS）有效期（秒）。
    /// </summary>
    public int DefaultUploadExpiresSeconds { get; set; } = 600;
}
