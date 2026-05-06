using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Fake.AspNetCore.ApplicationServiceConventions;

/// <summary>
/// details see：https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.Core/src/ApplicationModels/ControllerActionDescriptorProvider.cs
/// </summary>
public class ApplicationServiceActionDescriptorProvider : IActionDescriptorProvider
{
    // ControllerActionDescriptorProvider Order is -1000
    public int Order => -1000 + 10;

    public void OnProvidersExecuting(ActionDescriptorProviderContext context)
    {
    }

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        foreach (var action in context.Results.Where(x => x is ControllerActionDescriptor)
                     .Cast<ControllerActionDescriptor>())
        {
            var disableAbpFeaturesAttribute =
                action.ControllerTypeInfo.GetCustomAttribute<DisableFakeFeaturesAttribute>(true);
            if (disableAbpFeaturesAttribute is { DisableFilters: true })
            {
                action.FilterDescriptors.Remove(x =>
                    x.Filter is ServiceFilterAttribute serviceFilterAttribute &&
                    typeof(IFakeFilter).IsAssignableFrom(serviceFilterAttribute.ServiceType));
            }
        }
    }
}