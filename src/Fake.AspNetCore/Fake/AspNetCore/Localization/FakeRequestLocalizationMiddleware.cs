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