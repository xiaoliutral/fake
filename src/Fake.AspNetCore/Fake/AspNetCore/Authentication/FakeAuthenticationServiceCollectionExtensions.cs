using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Fake.AspNetCore.Authentication;

public static class FakeAuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// jwt鉴权，默认bearer
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureAuthenticationOptions"></param>
    /// <param name="configureJwtBearerOptions"></param>
    /// <returns></returns>
    /// <exception cref="FakeException"></exception>
    public static AuthenticationBuilder AddFakeJwtAuthentication(
        this IServiceCollection services,
        Action<AuthenticationOptions>? configureAuthenticationOptions = null,
        Action<JwtBearerOptions>? configureJwtBearerOptions = null)
    {
        var jwtConfiguration = services.GetConfiguration().GetSection(FakeJwtOptions.SectionName);
        services.Configure<FakeJwtOptions>(jwtConfiguration);
        var jwtOptions = jwtConfiguration.Get<FakeJwtOptions>()
                         ?? throw new ArgumentNullException($"未找到Jwt配置，请配置{FakeJwtOptions.SectionName}");

        ArgumentNullException.ThrowIfNull(services, nameof(services));
        AuthenticationBuilder authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            
            if (configureAuthenticationOptions == null) return;
            
            configureAuthenticationOptions.Invoke(options);
            services.Configure(configureAuthenticationOptions);
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ClockSkew = TimeSpan.FromMinutes(jwtOptions.ClockSkewMinutes)
            };

            if (configureJwtBearerOptions == null) return;
            
            configureJwtBearerOptions.Invoke(options);
            services.Configure(configureJwtBearerOptions);
        });
        ;
        return authenticationBuilder;
    }
}