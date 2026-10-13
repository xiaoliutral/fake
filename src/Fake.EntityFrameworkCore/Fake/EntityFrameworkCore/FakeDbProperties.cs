namespace Fake.EntityFrameworkCore;

/// <summary>
/// Fake 框架库表命名约定（对齐 ABP DbProperties）。
/// 默认前缀 <c>fk_</c>，便于与业务表区分；可在宿主启动早期通过配置覆盖。
/// </summary>
public static class FakeDbProperties
{
    public const string DefaultTablePrefix = "fk_";

    public const string TablePrefixConfigurationKey = "Fake:DbTablePrefix";

    /// <summary>
    /// 表名前缀。建议以下划线结尾（如 <c>fk_</c>）；设为空字符串可关闭前缀。
    /// </summary>
    public static string DbTablePrefix { get; set; } = DefaultTablePrefix;

    /// <summary>
    /// 可选 Schema（MySQL 通常为 null）。
    /// </summary>
    public static string? DbSchema { get; set; }

    /// <summary>
    /// 由无前缀短名生成物理表名，例如 <c>user</c> → <c>fk_user</c>。
    /// </summary>
    public static string GetTableName(string nameWithoutPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameWithoutPrefix);

        var prefix = DbTablePrefix;
        if (prefix.Length > 0 &&
            nameWithoutPrefix.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return nameWithoutPrefix;
        }

        return prefix + nameWithoutPrefix;
    }

    /// <summary>
    /// 从配置应用前缀（应在 DbContext 建模 / Migrate 之前调用）。
    /// </summary>
    public static void Configure(string? tablePrefix)
    {
        if (tablePrefix is null)
            return;

        DbTablePrefix = tablePrefix;
    }
}
