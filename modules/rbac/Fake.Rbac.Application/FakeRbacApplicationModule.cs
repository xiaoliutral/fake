using System.Text;
using Fake.Modularity;
using Fake.ObjectMapping.AutoMapper;
using Fake.Rbac.Application.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Fake.Rbac.Application;

[DependsOn(
    typeof(FakeRbacDomainModule),
    typeof(FakeObjectMappingAutoMapperModule)
)]
public class FakeRbacApplicationModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeAutoMapperOptions>(options =>
        {
            options.AddProfile<AutoMapper.RbacApplicationAutoMapperProfile>(validate: false);
        });

        var jwtConfiguration = context.Services.GetConfiguration().GetSection(JwtOptions.SectionName);
        context.Services.Configure<JwtOptions>(jwtConfiguration);
        var jwtOptions = jwtConfiguration.Get<JwtOptions>() ?? throw new FakeException($"请配置{JwtOptions.SectionName}");
        context.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
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
            });

        context.Services.AddAuthorization();
    }
}