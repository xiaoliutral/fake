using Fake.Domain.Entities.Auditing;

namespace Fake.FileManagement.Domain.FileAggregate;

public class StoredFile : FullAuditedAggregateRoot<Guid>
{
    public const int MaxFileNameLength = 256;
    public const int MaxObjectKeyLength = 512;
    public const int MaxContentTypeLength = 128;
    public const int MaxCategoryLength = 64;
    public const int MaxBizTypeLength = 64;
    public const int MaxBizIdLength = 64;

    /// <summary>
    /// 逻辑对象键（不含环境 KeyPrefix）。
    /// </summary>
    public string ObjectKey { get; private set; } = null!;

    public string FileName { get; private set; } = null!;

    public string? ContentType { get; private set; }

    public long Size { get; private set; }

    /// <summary>
    /// 业务分类，例如 avatar / waybill / document。
    /// </summary>
    public string Category { get; private set; } = null!;

    public string? BizType { get; private set; }

    public string? BizId { get; private set; }

    protected StoredFile()
    {
    }

    public StoredFile(
        string objectKey,
        string fileName,
        string category,
        long size,
        string? contentType = null,
        string? bizType = null,
        string? bizId = null)
    {
        SetObjectKey(objectKey);
        SetFileName(fileName);
        SetCategory(category);
        SetSize(size);
        ContentType = contentType;
        BizType = NormalizeOptional(bizType, MaxBizTypeLength, nameof(bizType));
        BizId = NormalizeOptional(bizId, MaxBizIdLength, nameof(bizId));
    }

    private void SetObjectKey(string objectKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        if (objectKey.Length > MaxObjectKeyLength)
        {
            throw new ArgumentException($"ObjectKey 长度不能超过 {MaxObjectKeyLength}", nameof(objectKey));
        }

        ObjectKey = objectKey;
    }

    private void SetFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (fileName.Length > MaxFileNameLength)
        {
            throw new ArgumentException($"FileName 长度不能超过 {MaxFileNameLength}", nameof(fileName));
        }

        FileName = fileName;
    }

    private void SetCategory(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        if (category.Length > MaxCategoryLength)
        {
            throw new ArgumentException($"Category 长度不能超过 {MaxCategoryLength}", nameof(category));
        }

        Category = category;
    }

    private void SetSize(long size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        Size = size;
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
}
