namespace Fake.Rbac.Application.Jwt;

/// <summary>
/// JWT 配置选项
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public static string SectionName = "JwtSettings";

    /// <summary>
    /// 密钥（至少32位字符）
    /// </summary>
    public string SecretKey { get; set; } = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";

    /// <summary>
    /// 签发者
    /// </summary>
    public string Issuer { get; set; } = "SimpleAdmin";

    /// <summary>
    /// 受众
    /// </summary>
    public string Audience { get; set; } = "SimpleAdminClient";

    /// <summary>
    /// Access Token 过期时间（分钟）
    /// </summary>
    public int ExpiryInMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh Token 过期时间（天）
    /// </summary>
    public int RefreshExpiryInDays { get; set; } = 7;

    /// <summary>
    /// 时钟偏移（分钟），用于处理服务器时间差异
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 5;
}