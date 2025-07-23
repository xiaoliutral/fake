using Fake;
using Fake.AspNetCore;
using Fake.AspNetCore.ExceptionHandling;
using Fake.AspNetCore.Localization;
using Fake.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

public static class FakeApplicationBuilderExtensions
{
    public const string FakeExceptionHandlingMiddlewareMarker = nameof(FakeExceptionHandlingMiddlewareMarker);
    public const string FakeRequestLocalizationMiddlewareMarker = nameof(FakeRequestLocalizationMiddlewareMarker);

    public static void InitializeApplication(this IApplicationBuilder app)
    {
        ThrowHelper.ThrowIfNull(app, nameof(app));

        var applicationBuilderAccessor = app.ApplicationServices.GetService<ObjectAccessor<IApplicationBuilder>>();
        if (applicationBuilderAccessor == null)
        {
            throw new FakeException($"请检查是否依赖{nameof(FakeAspNetCoreModule)}模块");
        }

        applicationBuilderAccessor.Value = app;

        var application = app.ApplicationServices.GetRequiredService<FakeApplication>();

        var applicationLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        applicationLifetime.ApplicationStopped.Register(() => application.Dispose());

        application.InitializeApplication(app.ApplicationServices);
    }

    /// <summary>
    /// 防呆设计，验证中间件是否已经注册
    /// </summary>
    /// <param name="app"></param>
    /// <param name="marker"></param>
    /// <returns></returns>
    public static bool VerifyMiddlewareAreRegistered(this IApplicationBuilder app, string marker)
    {
        ThrowHelper.ThrowIfNull(app, nameof(app));

        if (app.Properties.ContainsKey(marker))
        {
            return true;
        }

        app.Properties[marker] = true;
        return false;
    }
    
    public static IApplicationBuilder UseFakeExceptionHandling(this IApplicationBuilder app)
    {
        return app.VerifyMiddlewareAreRegistered(FakeExceptionHandlingMiddlewareMarker)
            ? app
            : app.UseMiddleware<FakeExceptionHandlingMiddleware>();
    }
    
    public static IApplicationBuilder UseFakeRequestLocalization(this IApplicationBuilder app)
    {
        return app.VerifyMiddlewareAreRegistered(FakeRequestLocalizationMiddlewareMarker)
            ? app
            : app.UseMiddleware<FakeRequestLocalizationMiddleware>();
    }
}