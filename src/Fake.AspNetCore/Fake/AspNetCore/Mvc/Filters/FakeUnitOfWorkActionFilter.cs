using Fake.UnitOfWork;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fake.AspNetCore.Mvc.Filters;

public class FakeUnitOfWorkActionFilter(IUnitOfWorkHelper unitOfWorkHelper, IUnitOfWorkManager unitOfWorkManager)
    : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (ShouldHandle(context))
        {
            await HandleUnitOfWorkAction(context, next);
            return;
        }

        await next();
    }

    protected virtual async Task HandleUnitOfWorkAction(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var methodInfo = context.ActionDescriptor.To<ControllerActionDescriptor>().MethodInfo;

        if (!unitOfWorkHelper.IsUnitOfWorkMethod(methodInfo, out var unitOfWorkAttribute)) await next();
        else
        {
            // 开工作单元
            using var unitOfWork = unitOfWorkManager.Begin(unitOfWorkAttribute);
            var result = await next();
            if (result.Exception == null || result.ExceptionHandled)
            {
                await unitOfWork.CompleteAsync(context.HttpContext.RequestAborted);
            }
            else
            {
                await unitOfWork.RollbackAsync(context.HttpContext.RequestAborted);
            }
        }
    }

    protected virtual bool ShouldHandle(ActionExecutingContext context)
    {
        return context.ActionDescriptor is ControllerActionDescriptor;
    }
}