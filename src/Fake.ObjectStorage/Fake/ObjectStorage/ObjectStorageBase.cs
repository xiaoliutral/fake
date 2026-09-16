using Microsoft.Extensions.Options;

namespace Fake.ObjectStorage;

public abstract class ObjectStorageBase(IOptions<FakeObjectStorageOptions> options) : IObjectStorage
{
    protected FakeObjectStorageOptions Options { get; } = options.Value;

    public abstract string Name { get; }

    public virtual bool SupportsClientDirectUpload => false;

    public async Task<ObjectStorageSaveResult> SaveAsync(
        ObjectStorageSaveArgs args,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(args.Content);

        var logicalKey = ObjectStoragePathHelper.NormalizeObjectKey(args.ObjectKey);
        Validate(args);

        var physicalKey = ObjectStoragePathHelper.ApplyKeyPrefix(logicalKey, Options.KeyPrefix);
        var normalizedArgs = new ObjectStorageSaveArgs
        {
            ObjectKey = physicalKey,
            Content = args.Content,
            ContentType = args.ContentType,
            Overwrite = args.Overwrite,
            Metadata = args.Metadata
        };

        await SaveCoreAsync(normalizedArgs, cancellationToken);

        var url = await GetUrlAsync(logicalKey, expires: null, cancellationToken);
        long? size = args.Content.CanSeek ? args.Content.Length : null;

        return new ObjectStorageSaveResult
        {
            // 对外仍返回逻辑 key，便于业务落库；物理前缀由存储层透明处理
            ObjectKey = logicalKey,
            Url = url,
            Size = size
        };
    }

    public Task<Stream> GetAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var physicalKey = ToPhysicalKey(objectKey);
        return GetCoreAsync(physicalKey, cancellationToken);
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var physicalKey = ToPhysicalKey(objectKey);
        return DeleteCoreAsync(physicalKey, cancellationToken);
    }

    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var physicalKey = ToPhysicalKey(objectKey);
        return ExistsCoreAsync(physicalKey, cancellationToken);
    }

    public Task<string> GetUrlAsync(
        string objectKey,
        TimeSpan? expires = null,
        CancellationToken cancellationToken = default)
    {
        var physicalKey = ToPhysicalKey(objectKey);
        var effectiveExpires = ResolveReadExpires(expires);
        return GetUrlCoreAsync(physicalKey, effectiveExpires, cancellationToken);
    }

    public Task<ObjectStoragePresignUploadResult> CreatePresignedUploadAsync(
        ObjectStoragePresignUploadArgs args,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        var logicalKey = ObjectStoragePathHelper.NormalizeObjectKey(args.ObjectKey);
        var physicalKey = ObjectStoragePathHelper.ApplyKeyPrefix(logicalKey, Options.KeyPrefix);
        var expires = ResolveUploadExpires(args.Expires);

        return CreatePresignedUploadCoreAsync(
            new ObjectStoragePresignUploadArgs
            {
                ObjectKey = physicalKey,
                ContentType = args.ContentType,
                Expires = expires
            },
            logicalKey,
            cancellationToken);
    }

    public Task<ObjectStorageStsUploadResult> CreateStsUploadAsync(
        ObjectStorageStsUploadArgs args,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        var logicalKey = ObjectStoragePathHelper.NormalizeObjectKey(args.ObjectKey);
        var physicalKey = ObjectStoragePathHelper.ApplyKeyPrefix(logicalKey, Options.KeyPrefix);
        var expires = ResolveUploadExpires(args.Expires);

        return CreateStsUploadCoreAsync(
            new ObjectStorageStsUploadArgs
            {
                ObjectKey = physicalKey,
                Expires = expires
            },
            logicalKey,
            cancellationToken);
    }

    protected string ToPhysicalKey(string objectKey)
    {
        var logicalKey = ObjectStoragePathHelper.NormalizeObjectKey(objectKey);
        return ObjectStoragePathHelper.ApplyKeyPrefix(logicalKey, Options.KeyPrefix);
    }

    protected TimeSpan ResolveReadExpires(TimeSpan? expires)
    {
        if (expires is { } ttl && ttl > TimeSpan.Zero)
        {
            return ttl;
        }

        if (Options.SignUrlsByDefault)
        {
            return TimeSpan.FromSeconds(Math.Max(60, Options.DefaultSignedUrlExpiresSeconds));
        }

        return TimeSpan.Zero;
    }

    protected TimeSpan ResolveUploadExpires(TimeSpan? expires)
    {
        if (expires is { } ttl && ttl > TimeSpan.Zero)
        {
            return ttl;
        }

        return TimeSpan.FromSeconds(Math.Max(60, Options.DefaultUploadExpiresSeconds));
    }

    protected abstract Task SaveCoreAsync(ObjectStorageSaveArgs args, CancellationToken cancellationToken);

    protected abstract Task<Stream> GetCoreAsync(string objectKey, CancellationToken cancellationToken);

    protected abstract Task DeleteCoreAsync(string objectKey, CancellationToken cancellationToken);

    protected abstract Task<bool> ExistsCoreAsync(string objectKey, CancellationToken cancellationToken);

    /// <summary>
    /// expires 为 Zero 表示不签名，返回永久/直链地址；大于 Zero 时生成临时签名。
    /// </summary>
    protected abstract Task<string> GetUrlCoreAsync(
        string objectKey,
        TimeSpan expires,
        CancellationToken cancellationToken);

    /// <summary>
    /// args.ObjectKey 已是物理 key；logicalObjectKey 用于返回给调用方。
    /// </summary>
    protected virtual Task<ObjectStoragePresignUploadResult> CreatePresignedUploadCoreAsync(
        ObjectStoragePresignUploadArgs args,
        string logicalObjectKey,
        CancellationToken cancellationToken) =>
        throw new FakeException($"{Name} 不支持预签名上传（CreatePresignedUpload）");

    protected virtual Task<ObjectStorageStsUploadResult> CreateStsUploadCoreAsync(
        ObjectStorageStsUploadArgs args,
        string logicalObjectKey,
        CancellationToken cancellationToken) =>
        throw new FakeException($"{Name} 不支持 STS 上传凭证（CreateStsUpload）");

    private void Validate(ObjectStorageSaveArgs args)
    {
        if (Options.MaxSizeBytes is > 0 &&
            args.Content.CanSeek &&
            args.Content.Length > Options.MaxSizeBytes.Value)
        {
            throw new FakeException($"文件大小不能超过 {Options.MaxSizeBytes.Value} 字节");
        }

        if (Options.AllowedContentTypes is { Length: > 0 } &&
            !string.IsNullOrWhiteSpace(args.ContentType) &&
            !Options.AllowedContentTypes.Contains(args.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new FakeException($"不支持的 ContentType：{args.ContentType}");
        }
    }
}
