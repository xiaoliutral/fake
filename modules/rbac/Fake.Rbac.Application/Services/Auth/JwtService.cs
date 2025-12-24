using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fake.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Fake.Rbac.Application.Services.Auth;

/// <summary>
/// JWT 服务实现
/// </summary>
public class JwtService : IJwtService, ITransientDependency
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public virtual string GenerateAccessToken(List<Claim> claims)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "SimpleAdmin";
        var audience = _configuration["JwtSettings:Audience"] ?? "SimpleAdminClient";
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        claims.Add(new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        claims.Add(new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public virtual string GenerateRefreshToken(List<Claim> claims)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "SimpleAdmin";
        var audience = _configuration["JwtSettings:Audience"] ?? "SimpleAdminClient";
        var refreshExpiryDays = int.Parse(_configuration["JwtSettings:RefreshExpiryInDays"] ?? "7");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        
        claims.Add(new("token_type", "refresh"));
        claims.Add(new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(refreshExpiryDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public virtual string? ValidateRefreshToken(string refreshToken)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "SimpleAdmin";
        var audience = _configuration["JwtSettings:Audience"] ?? "SimpleAdminClient";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(5)
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
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60");
        return expiryMinutes * 60;
    }
}
