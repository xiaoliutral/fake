namespace Fake.ObjectStorage.AliyunOss;

public class AliyunOssOptions
{
    public const string SectionName = "ObjectStorage:AliyunOss";

    public string Endpoint { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string AccessKeySecret { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 自定义域名或 CDN 地址。为空时使用默认 OSS 访问域名。
    /// </summary>
    public string? BaseUrl { get; set; }

    public bool UseHttps { get; set; } = true;
}
