using COSSTS;
using COSXML;
using COSXML.Auth;
using COSXML.Model.Object;
using COSXML.Model.Tag;
using Fake.ObjectStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fake.ObjectStorage.TencentCos;

public class TencentCosObjectStorage : ObjectStorageBase, IDisposable
{
    private static readonly string[] UploadActions =
    [
        "name/cos:PutObject",
        "name/cos:PostObject",
        "name/cos:InitiateMultipartUpload",
        "name/cos:ListMultipartUploads",
        "name/cos:ListParts",
        "name/cos:UploadPart",
        "name/cos:CompleteMultipartUpload",
        "name/cos:AbortMultipartUpload"
    ];

    private readonly TencentCosOptions _cos;
    private readonly CosXmlServer _client;
    private readonly ILogger<TencentCosObjectStorage> _logger;
    private readonly string _appId;
    private readonly string _publicBaseUrl;
    private readonly string? _customHost;

    public TencentCosObjectStorage(
        IOptions<FakeObjectStorageOptions> options,
        IOptions<TencentCosOptions> tencentCosOptions,
        ILogger<TencentCosObjectStorage> logger)
        : base(options)
    {
        _logger = logger;
        _cos = tencentCosOptions.Value;
        EnsureConfigured(_cos);

        _appId = ExtractAppId(_cos.Bucket);
        _publicBaseUrl = ResolvePublicBaseUrl(_cos);
        _customHost = Uri.TryCreate(_publicBaseUrl, UriKind.Absolute, out var baseUri)
            ? baseUri.Host
            : null;

        var config = new CosXmlConfig.Builder()
            .IsHttps(_cos.UseHttps)
            .SetRegion(_cos.Region)
            .SetAppid(_appId)
            .Build();

        var credentialSeconds = Math.Max(
            60,
            Math.Max(_cos.DurationSecond, Options.DefaultUploadExpiresSeconds));
        QCloudCredentialProvider credentialProvider = new DefaultQCloudCredentialProvider(
            _cos.SecretId,
            _cos.SecretKey,
            credentialSeconds);

        _client = new CosXmlServer(config, credentialProvider);
    }

    public override string Name => "TencentCos";

    public override bool SupportsClientDirectUpload => true;

    protected override Task SaveCoreAsync(ObjectStorageSaveArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!args.Overwrite && ObjectExists(args.ObjectKey))
        {
            throw new FakeException($"对象已存在：{args.ObjectKey}");
        }

        if (args.Content.CanSeek)
        {
            args.Content.Position = 0;
        }

        var request = new PutObjectRequest(_cos.Bucket, args.ObjectKey, args.Content);
        if (!string.IsNullOrWhiteSpace(args.ContentType))
        {
            request.SetRequestHeader("Content-Type", args.ContentType);
        }

        if (args.Metadata != null)
        {
            foreach (var (key, value) in args.Metadata)
            {
                request.SetRequestHeader($"x-cos-meta-{key}", value);
            }
        }

        _client.PutObject(request);
        return Task.CompletedTask;
    }

    protected override Task<Stream> GetCoreAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new GetObjectBytesRequest(_cos.Bucket, objectKey);
        var result = _client.GetObject(request);
        Stream stream = new MemoryStream(result.content);
        stream.Position = 0;
        return Task.FromResult(stream);
    }

    protected override Task DeleteCoreAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new DeleteObjectRequest(_cos.Bucket, objectKey);
        _client.DeleteObject(request);
        return Task.CompletedTask;
    }

    protected override Task<bool> ExistsCoreAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ObjectExists(objectKey));
    }

    protected override Task<string> GetUrlCoreAsync(
        string objectKey,
        TimeSpan expires,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 私有桶默认签名：即使上层未要求 expires，也签发临时读 URL
        if (expires <= TimeSpan.Zero && _cos.SignUrls)
        {
            expires = TimeSpan.FromSeconds(Math.Max(60, Options.DefaultSignedUrlExpiresSeconds));
        }

        if (expires > TimeSpan.Zero)
        {
            return Task.FromResult(GenerateSignedUrl(objectKey, "GET", expires, headers: null));
        }

        return Task.FromResult(ObjectStoragePathHelper.CombineUrl(_publicBaseUrl, objectKey));
    }

    protected override Task<ObjectStoragePresignUploadResult> CreatePresignedUploadCoreAsync(
        ObjectStoragePresignUploadArgs args,
        string logicalObjectKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expires = args.Expires ?? ResolveUploadExpires(null);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(args.ContentType))
        {
            headers["Content-Type"] = args.ContentType.Trim();
        }

        var uploadUrl = GenerateSignedUrl(
            args.ObjectKey,
            "PUT",
            expires,
            headers.Count > 0 ? headers : null);

        return Task.FromResult(new ObjectStoragePresignUploadResult
        {
            ObjectKey = logicalObjectKey,
            UploadUrl = uploadUrl,
            Method = "PUT",
            ContentType = string.IsNullOrWhiteSpace(args.ContentType) ? null : args.ContentType.Trim(),
            Headers = headers,
            ExpireAt = DateTime.UtcNow.Add(expires)
        });
    }

    protected override Task<ObjectStorageStsUploadResult> CreateStsUploadCoreAsync(
        ObjectStorageStsUploadArgs args,
        string logicalObjectKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expires = args.Expires ?? ResolveUploadExpires(null);
        var durationSeconds = (int)Math.Max(60, expires.TotalSeconds);

        var values = new Dictionary<string, object>
        {
            ["bucket"] = _cos.Bucket,
            ["region"] = _cos.Region,
            ["allowPrefix"] = args.ObjectKey,
            ["allowActions"] = UploadActions,
            ["durationSeconds"] = durationSeconds,
            ["secretId"] = _cos.SecretId,
            ["secretKey"] = _cos.SecretKey
        };

        Dictionary<string, object> credential;
        try
        {
            credential = STSClient.genCredential(values);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "申请 COS STS 临时密钥失败");
            throw new FakeException("申请 COS STS 临时上传凭证失败，请检查 SecretId/SecretKey 与 CAM 权限");
        }

        var credentials = ExtractCredentials(credential["Credentials"]);
        var startTime = ToUnixSeconds(credential.GetValueOrDefault("StartTime"));
        var expiredTime = ToUnixSeconds(credential.GetValueOrDefault("ExpiredTime"));
        if (startTime <= 0)
        {
            startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        if (expiredTime <= 0)
        {
            expiredTime = startTime + durationSeconds;
        }

        return Task.FromResult(new ObjectStorageStsUploadResult
        {
            ObjectKey = logicalObjectKey,
            PhysicalObjectKey = args.ObjectKey,
            Bucket = _cos.Bucket,
            Region = _cos.Region,
            TmpSecretId = credentials.tmpSecretId,
            TmpSecretKey = credentials.tmpSecretKey,
            SessionToken = credentials.token,
            StartTime = startTime,
            ExpiredTime = expiredTime
        });
    }

    public void Dispose()
    {
    }

    private string GenerateSignedUrl(
        string objectKey,
        string httpMethod,
        TimeSpan expires,
        Dictionary<string, string>? headers)
    {
        var signDuration = (long)Math.Ceiling(expires.TotalSeconds);
        if (signDuration <= 0)
        {
            signDuration = 60;
        }

        var preSignatureStruct = new PreSignatureStruct
        {
            appid = _appId,
            region = _cos.Region,
            bucket = _cos.Bucket,
            key = objectKey,
            httpMethod = httpMethod,
            isHttps = _cos.UseHttps,
            signDurationSecond = signDuration,
            headers = headers,
            queryParameters = null
        };

        // 自定义域名（CDN）必须写入 host，否则签名与访问域名不一致
        if (!string.IsNullOrWhiteSpace(_customHost)
            && !_customHost.EndsWith(".myqcloud.com", StringComparison.OrdinalIgnoreCase))
        {
            preSignatureStruct.host = _customHost;
            preSignatureStruct.signHost = true;
        }

        try
        {
            return _client.GenerateSignURL(preSignatureStruct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成 COS 预签名 URL 失败: {Method} {ObjectKey}", httpMethod, objectKey);
            throw new FakeException("生成 COS 预签名 URL 失败");
        }
    }

    private bool ObjectExists(string objectKey)
    {
        try
        {
            var request = new HeadObjectRequest(_cos.Bucket, objectKey);
            _client.HeadObject(request);
            return true;
        }
        catch (COSXML.CosException.CosServerException ex) when (ex.statusCode == 404)
        {
            return false;
        }
    }

    private static string ResolvePublicBaseUrl(TencentCosOptions cos)
    {
        if (!string.IsNullOrWhiteSpace(cos.BaseUrl))
        {
            return cos.BaseUrl.Trim().TrimEnd('/');
        }

        var scheme = cos.UseHttps ? "https" : "http";
        return $"{scheme}://{cos.Bucket}.cos.{cos.Region}.myqcloud.com";
    }

    private static void EnsureConfigured(TencentCosOptions cos)
    {
        if (string.IsNullOrWhiteSpace(cos.Region) ||
            string.IsNullOrWhiteSpace(cos.SecretId) ||
            string.IsNullOrWhiteSpace(cos.SecretKey) ||
            string.IsNullOrWhiteSpace(cos.Bucket))
        {
            throw new FakeException("腾讯云 COS 配置不完整，请检查 ObjectStorage:TencentCos");
        }

        if (!cos.Bucket.Contains('-', StringComparison.Ordinal))
        {
            throw new FakeException(
                "腾讯云 COS Bucket 须为 BucketName-AppId 格式（例如 examplebucket-1250000000），AppId 已从 CosConfig 移除");
        }
    }

    private static string ExtractAppId(string bucket)
    {
        var index = bucket.LastIndexOf('-');
        if (index < 0 || index == bucket.Length - 1)
        {
            throw new FakeException(
                "腾讯云 COS Bucket 须为 BucketName-AppId 格式（例如 examplebucket-1250000000）");
        }

        return bucket[(index + 1)..];
    }

    private static (string tmpSecretId, string tmpSecretKey, string token) ExtractCredentials(object? raw)
    {
        if (raw == null)
        {
            throw new FakeException("STS 返回 Credentials 为空");
        }

        var json = JsonConvert.SerializeObject(raw);
        var obj = JObject.Parse(json);
        var id = obj.Value<string>("TmpSecretId") ?? obj.Value<string>("tmpSecretId");
        var key = obj.Value<string>("TmpSecretKey") ?? obj.Value<string>("tmpSecretKey");
        var token = obj.Value<string>("Token") ?? obj.Value<string>("sessionToken") ?? obj.Value<string>("token");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(token))
        {
            throw new FakeException("STS 凭证字段不完整");
        }

        return (id, key, token);
    }

    private static long ToUnixSeconds(object? value) =>
        value switch
        {
            null => 0,
            long l => l,
            int i => i,
            string s when long.TryParse(s, out var n) => n,
            JValue j when j.Type == JTokenType.Integer => j.Value<long>(),
            _ => 0
        };
}
