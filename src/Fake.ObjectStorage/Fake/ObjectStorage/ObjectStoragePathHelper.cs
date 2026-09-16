namespace Fake.ObjectStorage;

public static class ObjectStoragePathHelper
{
    public static string NormalizeObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new FakeException($"{nameof(objectKey)} 不能为空");
        }

        var normalized = objectKey.Replace('\\', '/').Trim('/');
        if (normalized.Length == 0)
        {
            throw new FakeException($"{nameof(objectKey)} 不能为空");
        }

        if (normalized.Contains("..", StringComparison.Ordinal) ||
            normalized.Contains("//", StringComparison.Ordinal))
        {
            throw new FakeException($"{nameof(objectKey)} 非法：{objectKey}");
        }

        return normalized;
    }

    /// <summary>
    /// 将逻辑 objectKey 加上配置的 KeyPrefix，得到实际写入存储的物理 key。
    /// </summary>
    public static string ApplyKeyPrefix(string objectKey, string? keyPrefix)
    {
        var logicalKey = NormalizeObjectKey(objectKey);
        if (string.IsNullOrWhiteSpace(keyPrefix))
        {
            return logicalKey;
        }

        var prefix = NormalizeObjectKey(keyPrefix);
        return $"{prefix}/{logicalKey}";
    }

    public static string CombineUrl(string? baseUrl, string objectKey)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "/" + objectKey;
        }

        return baseUrl.TrimEnd('/') + "/" + objectKey;
    }
}
