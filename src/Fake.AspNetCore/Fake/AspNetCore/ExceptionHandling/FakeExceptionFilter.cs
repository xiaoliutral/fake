using Fake.ExceptionHandling;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fake.AspNetCore.ExceptionHandling;

public class FakeExceptionFilter(ILogger<FakeExceptionFilter> logger) : IAsyncExceptionFilter, IFakeFilter
{
    public virtual async Task OnExceptionAsync(ExceptionContext context)
    {
        if (!ShouldHandle(context)) return;

        await HandleAndWrapExceptionAsync(context);

        context.ExceptionHandled = true; // Handled!
    }

    protected virtual bool ShouldHandle(ExceptionContext context)
    {
        if (context.ExceptionHandled) return false;

        if (context.ActionDescriptor is ControllerActionDescriptor)
        {
            return true;
        }

        return false;
    }

    protected virtual async Task HandleAndWrapExceptionAsync(ExceptionContext context)
    {
        var httpContext = context.HttpContext;
        var exceptionHandlingOptions = httpContext.RequestServices
            .GetRequiredService<IOptions<FakeExceptionHandlingOptions>>()
            .Value;

        if (exceptionHandlingOptions.ShouldLogException(context.Exception))
        {
            logger.LogException(context.Exception);
        }

        await httpContext.RequestServices.GetRequiredService<IExceptionNotifier>()
            .NotifyAsync(new ExceptionNotificationContext(context.Exception, httpContext.RequestServices));


        if (context.HttpContext.Response.HasStarted)
        {
            logger.LogWarning("Exception occurred, but response has already started!");
        }

        var statusCodeFinder = httpContext.RequestServices.GetRequiredService<IHttpExceptionStatusCodeFinder>();

        var exceptionToErrorInfoConverter = httpContext.RequestServices
            .GetRequiredService<IException2ResponseConverter>();
        var remoteServiceErrorInfo =
            exceptionToErrorInfoConverter.Convert(context.Exception, exceptionHandlingOptions);

        context.HttpContext.Response.StatusCode = (int)statusCodeFinder.Find(httpContext, context.Exception);
        context.Result = new ObjectResult(remoteServiceErrorInfo);
    }
}