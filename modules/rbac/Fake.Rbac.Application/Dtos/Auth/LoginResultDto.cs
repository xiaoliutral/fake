namespace Fake.Rbac.Application.Dtos.Auth;

/// <summary>
/// 登录结果 DTO
/// </summary>
public class LoginResultDto
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// 令牌类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }
    
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }
}
