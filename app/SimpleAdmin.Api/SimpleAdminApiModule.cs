using Fake.AspNetCore;
using Fake.Autofac;
using Fake.AspNetCore.Mvc.Conventions;
using Fake.Modularity;
using Fake.Rbac.Application;
using Fake.Rbac.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Fake.AspNetCore.Auditing;

namespace SimpleAdmin.Api;

[DependsOn(
    typeof(FakeAspNetCoreModule),
    typeof(FakeAutofacModule),
    typeof(FakeRbacApplicationModule),
    typeof(FakeRbacInfrastructureModule)
)]
public class SimpleAdminApiModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        // 配置 CORS
        context.Services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:5281")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = context.Services.GetConfiguration();

        // 配置动态 API - 扫描 RBAC 应用服务
        services.Configure<ApplicationService2ControllerOptions>(options =>
        {
            options.RootPath = "rbac";
            options.ScanApplicationServices<FakeRbacApplicationModule>();
        });
        
        services.AddFakeExceptionFilter()
            .AddFakeValidationActionFilter()
            .AddFakeUnitOfWorkActionFilter()
            .AddFakeAspNetCoreAuditing();

        ConfigureJwtAndSwagger(configuration, services);
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
        var app = context.GetWebApplication();
        var env = context.GetEnvironment();

        // 开发环境配置
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Swagger
        app.UseFakeSwagger();
        // 异常处理
        app.UseFakeExceptionHandling();
        // CORS
        app.UseCors("Default");

        // 认证和授权
        app.UseAuthentication();
        app.UseAuthorization();
        // 路由
        app.UseRouting();
        // 端点映射
        app.MapControllers();
    }
    
    private static void ConfigureJwtAndSwagger(IConfiguration configuration, IServiceCollection services)
    {
        // 配置 JWT 认证
        var jwtSettings = configuration.GetSection("JWTSettings");
        var jwtSecret = jwtSettings["IssuerSigningKey"] ?? "3c1cbc3f546eda35168c3aa3cb91780fbe703f0996c1d133ea96dc85c70bbc0a";
        var jwtIssuer = jwtSettings["ValidIssuer"] ?? "SimpleAdmin";
        var jwtAudience = jwtSettings["ValidAudience"] ?? "SimpleAdmin";
        var clockSkew = int.Parse(jwtSettings["ClockSkew"] ?? "10");
        
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = bool.Parse(jwtSettings["ValidateIssuer"] ?? "true"),
                    ValidateAudience = bool.Parse(jwtSettings["ValidateAudience"] ?? "true"),
                    ValidateLifetime = bool.Parse(jwtSettings["ValidateLifetime"] ?? "true"),
                    ValidateIssuerSigningKey = bool.Parse(jwtSettings["ValidateIssuerSigningKey"] ?? "true"),
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.FromSeconds(clockSkew)
                };
            });

        services.AddAuthorization();

        // 添加 Fake Swagger 支持
        services.AddFakeSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "SimpleAdmin API", 
                Version = "v1",
                Description = "基于 Fake 框架的简单管理系统 API"
            });

            // 添加 JWT 认证配置
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    []
                }
            });
        });
    }
}