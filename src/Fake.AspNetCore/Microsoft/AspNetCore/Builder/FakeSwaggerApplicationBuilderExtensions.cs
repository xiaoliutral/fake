using Fake.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Microsoft.AspNetCore.Builder;

public static class FakeSwaggerApplicationBuilderExtensions
{
    public static IApplicationBuilder UseFakeSwagger(this IApplicationBuilder app,
        Action<SwaggerOptions>? setupAction = null,
        Action<SwaggerUIOptions>? uiSetupAction = null)
    {
        var isDevelopment = app.ApplicationServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
        var prefixValue = string.Empty;

        if (!isDevelopment)
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var prefix = (configuration.GetSection("App:RoutePrefix").Get<string>() ?? string.Empty).Trim('/');
            prefixValue = prefix.IsNullOrWhiteSpace() ? string.Empty : "/" + prefix;
        }


        //启用swagger中间件
        app.UseSwagger(options => { setupAction?.Invoke(options); });

        // 启用swagger-ui中间件，指定swagger json文件路径
        app.UseSwaggerUI(options =>
        {
            var apiDescriptionGroups = app.ApplicationServices
                .GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items;

            foreach (var description in apiDescriptionGroups)
            {
                options.SwaggerEndpoint($"{prefixValue}/swagger/{description.GroupName}/swagger.json",
                    description.GroupName);
            }

            uiSetupAction?.Invoke(options);
        });

        return app;
    }
}