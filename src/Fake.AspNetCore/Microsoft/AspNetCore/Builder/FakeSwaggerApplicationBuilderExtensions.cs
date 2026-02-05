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
        //启用swagger中间件
        app.UseSwagger(options =>
        {
            options.PreSerializeFilters.Add((doc, req) =>
            {
                var basePath = req.PathBase.HasValue ? req.PathBase.Value : "";
                doc.Servers = new List<OpenApiServer>
                {
                    new() { Url = $"{req.Scheme}://{req.Host.Value}{basePath}" }
                };
            });
            setupAction?.Invoke(options);
        });

        // 启用swagger-ui中间件，指定swagger json文件路径
        app.UseSwaggerUI(options =>
        {
            var apiDescriptionGroups = app.ApplicationServices
                .GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items;

            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var pathBase = (configuration["ASPNETCORE_PATHBASE"] ?? string.Empty).Trim('/');
            var pathBaseValue = pathBase.IsNullOrWhiteSpace() ? string.Empty : "/" + pathBase;
            
            foreach (var description in apiDescriptionGroups)
            {
                options.SwaggerEndpoint($"{pathBaseValue}/swagger/{description.GroupName}/swagger.json",
                    description.GroupName);
            }
            
            options.RoutePrefix = string.Empty;
            options.DocExpansion(DocExpansion.None); // 设置swagger收起所有标签
            options.DefaultModelExpandDepth(2);  // 设置模型参数展开层架

            uiSetupAction?.Invoke(options);
        });

        return app;
    }
}