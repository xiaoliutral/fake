namespace Fake.ObjectStorage;

/// <summary>
/// 对象存储门面，业务层依赖此接口。
/// </summary>
public interface IObjectStorage
{
    /// <summary>Provider 名称，例如 Local / TencentCos / AliyunOss。</summary>
    string Name { get; }

    /// <summary>是否支持前端直传（预签名 PUT / STS）。</summary>
    bool SupportsClientDirectUpload { get; }

    Task<ObjectStorageSaveResult> SaveAsync(
        ObjectStorageSaveArgs args,
        CancellationToken cancellationToken = default);

    Task<Stream> GetAsync(string objectKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取访问地址。
    /// expires 有值时生成签名 URL；expires 为空且 <see cref="FakeObjectStorageOptions.SignUrlsByDefault"/> 为 true 时，
    /// 使用默认读签名有效期（私有桶场景）。
    /// </summary>
    Task<string> GetUrlAsync(
        string objectKey,
        TimeSpan? expires = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 签发预签名 PUT 上传地址。不支持时抛出 <see cref="FakeException"/>。
    /// </summary>
    Task<ObjectStoragePresignUploadResult> CreatePresignedUploadAsync(
        ObjectStoragePresignUploadArgs args,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 签发 STS 临时上传凭证。不支持时抛出 <see cref="FakeException"/>。
    /// </summary>
    Task<ObjectStorageStsUploadResult> CreateStsUploadAsync(
        ObjectStorageStsUploadArgs args,
        CancellationToken cancellationToken = default);
}
