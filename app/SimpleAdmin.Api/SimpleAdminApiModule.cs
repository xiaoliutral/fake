using Fake.AspNetCore;
using Fake.Autofac;
using Fake.Modularity;
using Fake.Rbac.Application;
using Fake.Rbac.Infrastructure;
using Fake.AspNetCore.Auditing;
using Fake.AspNetCore.Mvc;

namespace SimpleAdmin.Api;

[DependsOn(
    typeof(FakeAutofacModule),
    typeof(FakeRbacApplicationModule),
    typeof(FakeRbacInfrastructureModule)
)]
public class SimpleAdminApiModule : FakeModule
{
    private const string DefaultCorsPolicyName = "default";

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = context.Services.GetConfiguration();

        // 配置动态 API - 扫描 RBAC 应用服务
        services.Configure<FakeAspNetCoreMvcOptions>(options =>
        {
            options.ApplicationServices2Controller<FakeRbacApplicationModule>(settings =>
            {
                settings.RootPath = "rbac";
            });
        });

        services.AddFakeAspNetCoreAuditing();

        // 添加 Fake Swagger 支持
        services.AddFakeSwaggerGen(addSecurity: true);

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultCorsPolicyName, builder =>
            {
                builder.WithOrigins(configuration.GetSection("App:CorsOrigins").Get<string[]>() ?? [])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
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
}