using Fake.Application;
using Fake.Helpers;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fake.AspNetCore.Mvc.Conventions;

public class ApplicationService2ControllerConvention(
    IOptions<FakeAspNetCoreMvcOptions> options,
    IApplicationServiceActionHelper applicationServiceActionHelper
) : IApplicationModelConvention
{
    public ILogger<ApplicationService2ControllerConvention> Logger { get; set; } =
        NullLogger<ApplicationService2ControllerConvention>.Instance;

    protected FakeAspNetCoreMvcOptions Options { get; } = options.Value;

    static string[] CommonPostfixes { get; set; } = ["ApplicationService", "AppService", "Service"];

    public virtual void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var controllerType = controller.ControllerType.AsType();

            var setting = GetControllerSettingOrNull(controllerType);

            if (controllerType.IsAssignableTo<IApplicationService>())
            {
                controller.ControllerName = controller.ControllerName.RemovePostfix(CommonPostfixes);
                setting?.ControllerModelConfigureAction?.Invoke(controller);
                ConfigureRemoteService(controller);
            }
        }
    }

    protected virtual void ConfigureRemoteService(ControllerModel controller)
    {
        ConfigureApiExplorer(controller);
        ConfigureSelector(controller);
        ConfigureParameters(controller);
    }

    protected virtual void ConfigureApiExplorer(ControllerModel controller)
    {
        if (controller.ApiExplorer.GroupName.IsNullOrEmpty())
        {
            controller.ApiExplorer.GroupName = controller.ControllerName;
        }
    }

    protected virtual void ConfigureSelector(ControllerModel controller)
    {
        var setting = GetControllerSettingOrNull(controller.ControllerType.AsType());
        var rootPath = setting?.RootPath ?? "api";

        foreach (var action in controller.Actions)
        {
            var httpVerb = applicationServiceActionHelper.GetHttpVerb(action);
            var route = applicationServiceActionHelper.GetRoute(action, httpVerb, rootPath);
            var routeMode = new AttributeRouteModel(new RouteAttribute(route));
            var selectorModel = new SelectorModel
            {
                AttributeRouteModel = routeMode,
                ActionConstraints = { new HttpMethodActionConstraint(new[] { httpVerb }) }
            };
            if (!action.Selectors.Any())
            {
                action.Selectors.Add(selectorModel);
            }
            else
            {
                // normal selector
                foreach (var selector in action.Selectors)
                {
                    selector.AttributeRouteModel ??= routeMode;

                    if (!selector.ActionConstraints.OfType<HttpMethodActionConstraint>().Any())
                    {
                        selector.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { httpVerb }));
                    }
                }
            }
        }
    }

    protected virtual void ConfigureParameters(ControllerModel controller)
    {
        /* Default binding system of Asp.Net Core for a parameter
         * 1. Form values
         * 2. Route values.
         * 3. Query string.
         */

        foreach (var action in controller.Actions)
        {
            foreach (var prm in action.Parameters)
            {
                if (prm.BindingInfo != null)
                {
                    continue;
                }

                if (!TypeHelper.IsBaseType(prm.ParameterInfo.ParameterType, includeEnums: true))
                {
                    if (CanUseFormBodyBinding(action, prm))
                    {
                        prm.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromBodyAttribute() });
                    }
                }
            }
        }
    }

    protected virtual bool CanUseFormBodyBinding(ActionModel action, ParameterModel parameter)
    {
        //We want to use "id" as path parameter, not body!
        if (parameter.ParameterName == "id")
        {
            return false;
        }

        foreach (var selector in action.Selectors)
        {
            foreach (var actionConstraint in selector.ActionConstraints)
            {
                var httpMethodActionConstraint = actionConstraint as HttpMethodActionConstraint;
                if (httpMethodActionConstraint == null)
                {
                    continue;
                }

                if (httpMethodActionConstraint.HttpMethods.All(hm => hm.IsIn("GET", "DELETE", "TRACE", "HEAD")))
                {
                    return false;
                }
            }
        }

        return true;
    }

    protected virtual ApplicationService2ControllerSetting? GetControllerSettingOrNull(Type controllerType)
    {
        return Options.ConventionalControllerSettings
            .FirstOrDefault(controllerSetting =>
                controllerSetting.ControllerTypes.Contains(controllerType));
    }
}