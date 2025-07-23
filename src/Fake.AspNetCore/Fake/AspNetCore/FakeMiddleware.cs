using System.Reflection;
using Fake.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Fake.AspNetCore;

public abstract class FakeMiddleware : IMiddleware
{
    protected virtual Task<bool> ShouldSkipAsync(HttpContext context, RequestDelegate next)
    {
        var endpoint = context.GetEndpoint();
        var controllerActionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        var disableAbpFeaturesAttribute = controllerActionDescriptor?.ControllerTypeInfo
            .GetCustomAttribute<DisableFakeFeaturesAttribute>();
        return Task.FromResult(disableAbpFeaturesAttribute is { DisableMiddleware: true });
    }

    public abstract Task InvokeAsync(HttpContext context, RequestDelegate next);
}