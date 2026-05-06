using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fake.AspNetCore.Authentication;
using Fake.DependencyInjection;
using Fake.Rbac.Application.Services;
using Fake.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fake.Rbac.Application.Jwt;

/// <summary>
/// JWT 服务实现
/// </summary>
public class JwtService(IOptions<FakeJwtOptions> jwtOptions, UserService userService) : IJwtService, ITransientDependency
{
    private readonly FakeJwtOptions _fakeJwtOptions = jwtOptions.Value;

    public virtual string GenerateAccessToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_fakeJwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        claims.Add(new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        claims.Add(new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

        var token = new JwtSecurityToken(
            issuer: _fakeJwtOptions.Issuer,
            audience: _fakeJwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_fakeJwtOptions.ExpiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public virtual string GenerateRefreshToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_fakeJwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        claims.Add(new("token_type", "refresh"));
        claims.Add(new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        var token = new JwtSecurityToken(
            issuer: _fakeJwtOptions.Issuer,
            audience: _fakeJwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_fakeJwtOptions.RefreshExpiryInDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public virtual string? ValidateRefreshToken(string refreshToken)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_fakeJwtOptions.SecretKey));
        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _fakeJwtOptions.Issuer,
            ValidAudience = _fakeJwtOptions.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(_fakeJwtOptions.ClockSkewMinutes)
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
        return _fakeJwtOptions.ExpiryInMinutes * 60;
    }

    public virtual async Task<List<Claim>> GenerateClaimsByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        // 获取用户角色
        var roles = await userService.GetUserRolesAsync(userId, cancellationToken);
        var roleCodes = roles.Select(r => r.Code).ToList();
        
        var user = await userService.GetAsync(userId, cancellationToken);
        
        var claims = new List<Claim>
        {
            new(FakeClaimTypes.UserId, userId.ToString()),
            new(FakeClaimTypes.UserName, user.Name),
        };

        // 添加角色声明
        foreach (var role in roleCodes)
        {
            claims.Add(new Claim(FakeClaimTypes.Role, role));
        }

        return claims;
    }
}
