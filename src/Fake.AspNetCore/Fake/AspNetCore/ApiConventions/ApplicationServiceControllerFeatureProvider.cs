using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Fake.AspNetCore.ApiConventions;

public class ApplicationServiceControllerFeatureProvider(IFakeApplication application) : ControllerFeatureProvider
{
    // service as controller
    protected override bool IsController(TypeInfo typeInfo)
    {
        return application.ServiceProvider
            .GetRequiredService<IOptions<FakeAspNetCoreMvcOptions>>().Value
            .ControllerSettings
            .Any(x => x.ControllerTypes.Contains(typeInfo.AsType()));
    }
}