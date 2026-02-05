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
            setupAction?.Invoke(options);
        });

        // 启用swagger-ui中间件，指定swagger json文件路径
        app.UseSwaggerUI(options =>
        {
            var apiDescriptionGroups = app.ApplicationServices
                .GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items;

            foreach (var description in apiDescriptionGroups)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName);
            }
            
            options.RoutePrefix = string.Empty;
            options.DocExpansion(DocExpansion.None); // 设置swagger收起所有标签
            options.DefaultModelExpandDepth(2);  //设置参数展开层架，这里是5层

            uiSetupAction?.Invoke(options);
        });

        return app;
    }
}