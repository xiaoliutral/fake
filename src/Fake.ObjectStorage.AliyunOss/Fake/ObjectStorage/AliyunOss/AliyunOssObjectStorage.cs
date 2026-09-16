using Aliyun.OSS;
using Fake.ObjectStorage;
using Microsoft.Extensions.Options;

namespace Fake.ObjectStorage.AliyunOss;

public class AliyunOssObjectStorage(
    IOptions<FakeObjectStorageOptions> options,
    IOptions<AliyunOssOptions> aliyunOssOptions)
    : ObjectStorageBase(options)
{
    private readonly AliyunOssOptions _oss = aliyunOssOptions.Value;

    public override string Name => "AliyunOss";

    public override bool SupportsClientDirectUpload => true;

    protected override Task SaveCoreAsync(ObjectStorageSaveArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConfigured(_oss);

        var client = CreateClient(_oss);

        if (!args.Overwrite && client.DoesObjectExist(_oss.BucketName, args.ObjectKey))
        {
            throw new FakeException($"对象已存在：{args.ObjectKey}");
        }

        if (args.Content.CanSeek)
        {
            args.Content.Position = 0;
        }

        var metadata = new ObjectMetadata();
        if (!string.IsNullOrWhiteSpace(args.ContentType))
        {
            metadata.ContentType = args.ContentType;
        }

        if (args.Metadata != null)
        {
            foreach (var (key, value) in args.Metadata)
            {
                metadata.UserMetadata[key] = value;
            }
        }

        client.PutObject(_oss.BucketName, args.ObjectKey, args.Content, metadata);
        return Task.CompletedTask;
    }

    protected override Task<Stream> GetCoreAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConfigured(_oss);

        var client = CreateClient(_oss);
        var result = client.GetObject(_oss.BucketName, objectKey);
        return Task.FromResult(result.Content);
    }

    protected override Task DeleteCoreAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConfigured(_oss);

        var client = CreateClient(_oss);
        client.DeleteObject(_oss.BucketName, objectKey);
        return Task.CompletedTask;
    }

    protected override Task<bool> ExistsCoreAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConfigured(_oss);

        var client = CreateClient(_oss);
        return Task.FromResult(client.DoesObjectExist(_oss.BucketName, objectKey));
    }

    protected override Task<string> GetUrlCoreAsync(
        string objectKey,
        TimeSpan expires,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConfigured(_oss);

        if (expires > TimeSpan.Zero)
        {
            var client = CreateClient(_oss);
            var uri = client.GeneratePresignedUri(_oss.BucketName, objectKey, DateTime.UtcNow.Add(expires));
            return Task.FromResult(uri.ToString());
        }

        if (!string.IsNullOrWhiteSpace(_oss.BaseUrl))
        {
            return Task.FromResult(ObjectStoragePathHelper.CombineUrl(_oss.BaseUrl, objectKey));
        }

        var scheme = _oss.UseHttps ? "https" : "http";
        var endpoint = _oss.Endpoint.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Trim('/');
        var url = $"{scheme}://{_oss.BucketName}.{endpoint}/{objectKey}";
        return Task.FromResult(url);
    }

    protected override Task<ObjectStoragePresignUploadResult> CreatePresignedUploadCoreAsync(
        ObjectStoragePresignUploadArgs args,
        string logicalObjectKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConfigured(_oss);

        var expires = args.Expires ?? ResolveUploadExpires(null);
        var client = CreateClient(_oss);
        var uri = client.GeneratePresignedUri(
            _oss.BucketName,
            args.ObjectKey,
            DateTime.UtcNow.Add(expires),
            SignHttpMethod.Put);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(args.ContentType))
        {
            headers["Content-Type"] = args.ContentType.Trim();
        }

        return Task.FromResult(new ObjectStoragePresignUploadResult
        {
            ObjectKey = logicalObjectKey,
            UploadUrl = uri.ToString(),
            Method = "PUT",
            ContentType = string.IsNullOrWhiteSpace(args.ContentType) ? null : args.ContentType.Trim(),
            Headers = headers,
            ExpireAt = DateTime.UtcNow.Add(expires)
        });
    }

    protected override Task<ObjectStorageStsUploadResult> CreateStsUploadCoreAsync(
        ObjectStorageStsUploadArgs args,
        string logicalObjectKey,
        CancellationToken cancellationToken) =>
        throw new FakeException("AliyunOss 暂未内置 STS（AssumeRole）。请使用 CreatePresignedUpload，或自行对接 RAM STS 后扩展。");

    private static OssClient CreateClient(AliyunOssOptions oss)
    {
        var endpoint = oss.Endpoint;
        if (!endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = (oss.UseHttps ? "https://" : "http://") + endpoint;
        }

        return new OssClient(endpoint, oss.AccessKeyId, oss.AccessKeySecret);
    }

    private static void EnsureConfigured(AliyunOssOptions oss)
    {
        if (string.IsNullOrWhiteSpace(oss.Endpoint) ||
            string.IsNullOrWhiteSpace(oss.AccessKeyId) ||
            string.IsNullOrWhiteSpace(oss.AccessKeySecret) ||
            string.IsNullOrWhiteSpace(oss.BucketName))
        {
            throw new FakeException("阿里云 OSS 配置不完整，请检查 ObjectStorage:AliyunOss");
        }
    }
}
