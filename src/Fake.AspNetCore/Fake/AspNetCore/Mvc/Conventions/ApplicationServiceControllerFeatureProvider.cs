using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Fake.AspNetCore.Mvc.Conventions;

public class ApplicationServiceControllerFeatureProvider(IFakeApplication application) : ControllerFeatureProvider
{
    // service as controller
    protected override bool IsController(TypeInfo typeInfo)
    {
        return application.ServiceProvider
            .GetRequiredService<IOptions<FakeAspNetCoreMvcOptions>>().Value
            .ConventionalControllerSettings
            .Any(x => x.ControllerTypes.Contains(typeInfo.AsType()));
    }
}