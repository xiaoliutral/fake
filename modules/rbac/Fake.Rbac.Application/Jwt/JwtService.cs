using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fake.DependencyInjection;
using Fake.Rbac.Application.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fake.Rbac.Application.Jwt;

/// <summary>
/// JWT 服务实现
/// </summary>
public class JwtService(IOptions<JwtOptions> jwtOptions, IUserService userService) : IJwtService, ITransientDependency
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public virtual string GenerateAccessToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        claims.Add(new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        claims.Add(new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public virtual string GenerateRefreshToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        claims.Add(new("token_type", "refresh"));
        claims.Add(new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_jwtOptions.RefreshExpiryInDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public virtual string? ValidateRefreshToken(string refreshToken)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(_jwtOptions.ClockSkewMinutes)
        };

        var principal = tokenHandler.ValidateToken(refreshToken, validationParameters, out var validatedToken);
            
        // 验证是否是 refresh token
        var tokenTypeClaim = principal.FindFirst("token_type");
        if (tokenTypeClaim?.Value != "refresh")
        {
            return null;
        }

        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public virtual int GetExpiresInSeconds()
    {
        return _jwtOptions.ExpiryInMinutes * 60;
    }

    public virtual async Task<List<Claim>> GenerateClaimsByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        // 获取用户角色
        var roles = await userService.GetUserRolesAsync(userId, cancellationToken);
        var roleCodes = roles.Select(r => r.Code).ToList();
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
        };

        // 添加角色声明
        foreach (var role in roleCodes)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }
}
