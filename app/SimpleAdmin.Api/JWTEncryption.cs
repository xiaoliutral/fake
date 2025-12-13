using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SimpleAdmin.Api;

public class JWTEncryption
{
    /// <summary>生成Token验证参数</summary>
    /// <param name="jwtSettings"></param>
    /// <returns></returns>
    public static TokenValidationParameters CreateTokenValidationParameters(
        JWTSettingsOptions jwtSettings)
    {
        return new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.IssuerSigningKey)),
            ValidateIssuer = jwtSettings.ValidateIssuer,
            ValidIssuer = jwtSettings.ValidIssuer,
            ValidateAudience = jwtSettings.ValidateAudience,
            ValidAudience = jwtSettings.ValidAudience,
            ValidateLifetime = jwtSettings.ValidateLifetime,
            ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkew),
            RequireExpirationTime = jwtSettings.RequireExpirationTime
        };
    }
}