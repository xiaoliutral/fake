using Fake.AspNetCore.Mvc.Validation;
using Fake.Helpers;
using Fake.Validation;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fake.AspNetCore.Mvc.Filters;

public class FakeValidationActionFilter : IAsyncActionFilter
{
    public virtual async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (ShouldHandle(context))
        {
            context.HttpContext.RequestServices.GetRequiredService<IModelStateValidator>().Validate(context.ModelState);
        }

        await next();
    }

    protected virtual void HandleModelError(ActionExecutingContext context)
    {
        var message = context.ModelState.Keys
            .SelectMany(key => context.ModelState[key]!.Errors
                .Select(error => error.ErrorMessage))
            .JoinAsString("\n");

        throw new FakeValidationException(message);
    }

    protected virtual bool ShouldHandle(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
        {
            return false;
        }

        if (ReflectionHelper.GetAttributeOrDefault<DisableValidationAttribute>(controllerActionDescriptor.MethodInfo) !=
            null)
        {
            return false;
        }

        return true;
    }
}