using System.Security.Claims;

namespace Fake.Rbac.Application.Jwt;

/// <summary>
/// JWT 服务接口
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// 生成访问令牌
    /// </summary>
    string GenerateAccessToken(List<Claim> claims);
    
    /// <summary>
    /// 生成刷新令牌
    /// </summary>
    string GenerateRefreshToken(List<Claim> claims);
    
    /// <summary>
    /// 验证刷新令牌并返回用户ID
    /// </summary>
    string? ValidateRefreshToken(string refreshToken);
    
    /// <summary>
    /// 获取令牌过期时间（秒）
    /// </summary>
    int GetExpiresInSeconds();

    /// <summary>
    /// 生成claims
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<Claim>> GenerateClaimsByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
