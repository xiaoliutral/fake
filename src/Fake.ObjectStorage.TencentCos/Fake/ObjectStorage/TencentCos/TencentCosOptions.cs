namespace Fake.ObjectStorage.TencentCos;

public class TencentCosOptions
{
    public const string SectionName = "ObjectStorage:TencentCos";

    public string Region { get; set; } = string.Empty;

    public string SecretId { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Bucket 全称，格式必须为 BucketName-AppId（例如 examplebucket-1250000000）。
    /// AppId 已从 CosConfig 移除，须包含在本参数中。
    /// </summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// 自定义域名或 CDN 地址。为空时使用默认 COS 访问域名。
    /// </summary>
    public string? BaseUrl { get; set; }

    public bool UseHttps { get; set; } = true;

    /// <summary>
    /// 私有桶：GetUrl 在未显式要求 expires 时也签发临时读签名。默认 true。
    /// 桶已公有读时可设 false。
    /// </summary>
    public bool SignUrls { get; set; } = true;

    /// <summary>
    /// 临时密钥有效期（秒），用于 SDK 凭证。
    /// </summary>
    public long DurationSecond { get; set; } = 600;
}
