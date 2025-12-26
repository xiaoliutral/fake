using System.Text;
using Fake;
using Fake.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class FakeAuthenticationServiceCollectionExtensions
{
    public static AuthenticationBuilder AddFakeJwtAuthentication(
        this IServiceCollection services,
        Action<AuthenticationOptions>? configureAuthenticationOptions = null,
        Action<JwtBearerOptions>? configureJwtBearerOptions = null)
    {
        var jwtConfiguration = services.GetConfiguration().GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtConfiguration);
        var jwtOptions = jwtConfiguration.Get<JwtOptions>() ?? throw new FakeException($"请配置{JwtOptions.SectionName}");

        ArgumentNullException.ThrowIfNull(services, nameof(services));
        AuthenticationBuilder authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            if (configureAuthenticationOptions != null)
            {
                configureAuthenticationOptions.Invoke(options);
                services.Configure(configureAuthenticationOptions);
            }
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
            
            if (configureJwtBearerOptions != null)
            {
                configureJwtBearerOptions.Invoke(options);
                services.Configure(configureJwtBearerOptions);
            }
        });
        ;
        return authenticationBuilder;
    }
}