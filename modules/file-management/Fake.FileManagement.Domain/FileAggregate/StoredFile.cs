using Fake.Domain.Entities.Auditing;

namespace Fake.FileManagement.Domain.FileAggregate;

/// <summary>
/// 通用文件元数据。对象存储 ObjectKey = <see cref="Id"/>，不含业务外键。
/// </summary>
public class StoredFile : FullAuditedAggregateRoot<Guid>
{
    public const int MaxFileNameLength = 256;
    public const int MaxContentTypeLength = 128;
    public const int MaxStorageSourceLength = 64;
    
    public string ObjectKey => Id.ToString();

    public string FileName { get; private set; } = null!;

    public string? ContentType { get; private set; }

    public long Size { get; private set; }

    /// <summary>
    /// 存储源名称。一期默认 default；多源时用于找回写入位置。
    /// </summary>
    public string StorageSource { get; private set; } = "default";

    public StoredFileStatus Status { get; private set; }

    protected StoredFile()
    {
    }

    public StoredFile(
        string fileName,
        StoredFileStatus status,
        long size = 0,
        string? contentType = null,
        string storageSource = "default")
    {
        SetFileName(fileName);
        SetSize(size);
        ContentType = NormalizeOptional(contentType, MaxContentTypeLength, nameof(contentType));
        SetStorageSource(storageSource);
        Status = status;
    }

    public void MarkAvailable(string fileName, long size, string? contentType)
    {
        if (Status == StoredFileStatus.Available)
        {
            return;
        }

        SetFileName(fileName);
        SetSize(size);
        ContentType = NormalizeOptional(contentType, MaxContentTypeLength, nameof(contentType));
        Status = StoredFileStatus.Available;
    }

    private void SetFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        fileName = fileName.Trim();
        if (fileName.Length > MaxFileNameLength)
        {
            throw new ArgumentException($"FileName 长度不能超过 {MaxFileNameLength}", nameof(fileName));
        }

        FileName = fileName;
    }

    private void SetSize(long size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        Size = size;
    }

    private void SetStorageSource(string storageSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageSource);
        storageSource = storageSource.Trim();
        if (storageSource.Length > MaxStorageSourceLength)
        {
            throw new ArgumentException($"StorageSource 长度不能超过 {MaxStorageSourceLength}", nameof(storageSource));
        }

        StorageSource = storageSource;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"{paramName} 长度不能超过 {maxLength}", paramName);
        }

        return value;
    }

    public static string GenerateObjectKey()
    {
        return Guid.NewGuid().ToString();
    }
}
