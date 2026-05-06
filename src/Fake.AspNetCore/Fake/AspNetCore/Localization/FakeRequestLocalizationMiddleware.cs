using Fake.DependencyInjection;
using Fake.Localization;

namespace Fake.AspNetCore.Localization;

public class FakeRequestLocalizationMiddleware : FakeMiddleware, ITransientDependency
{
    public override async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var language = context.Request.Headers["Accept-Language"].FirstOrDefault() ?? "zh";
        using var _ = CultureHelper.UseCulture(language);
        await next.Invoke(context);
    }
}

public static class FakeRequestLocalizationMiddlewareExtensions
{
    public const string FakeRequestLocalizationMiddlewareMarker = nameof(FakeRequestLocalizationMiddlewareMarker);
       
    public static IApplicationBuilder UseFakeRequestLocalization(this IApplicationBuilder app)
    {
        return app.VerifyMiddlewareAreRegistered(FakeRequestLocalizationMiddlewareMarker)
            ? app
            : app.UseMiddleware<FakeRequestLocalizationMiddleware>();
    }
}