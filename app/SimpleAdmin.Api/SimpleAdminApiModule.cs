using Fake.AspNetCore;
using Fake.Autofac;
using Fake.Modularity;
using Fake.Rbac.Application;
using Fake.Rbac.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Fake.AspNetCore.Auditing;
using Fake.AspNetCore.Mvc;
using Fake.Rbac.Application.Jwt;

namespace SimpleAdmin.Api;

[DependsOn(
    typeof(FakeAutofacModule),
    typeof(FakeRbacApplicationModule),
    typeof(FakeRbacInfrastructureModule)
)]
public class SimpleAdminApiModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
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

        // 配置动态 API - 扫描 RBAC 应用服务
        services.Configure<FakeAspNetCoreMvcOptions>(options =>
        {
            options.ApplicationServices2Controller<FakeRbacApplicationModule>(settings =>
            {
                settings.RootPath = "rbac";
            });
        });

        services.AddFakeAspNetCoreAuditing();

        ConfigureSwagger(services);
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

        // 静态文件服务（用于头像等上传文件）
        app.UseStaticFiles();

        // 路由 - 必须在认证和授权之前
        app.UseRouting();

        // 认证和授权
        app.UseAuthentication();
        app.UseAuthorization();

        // 端点映射
        app.MapControllers();
    }

    private static void ConfigureSwagger(IServiceCollection services)
    {
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
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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