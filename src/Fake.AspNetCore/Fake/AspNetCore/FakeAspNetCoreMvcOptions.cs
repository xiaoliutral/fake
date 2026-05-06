using Fake.AspNetCore.ApplicationServiceConventions;
using Fake.Modularity;

namespace Fake.AspNetCore;

public class FakeAspNetCoreMvcOptions
{
    public List<ApplicationService2ControllerSetting> ControllerSettings { get; } = new();

    public void ApplicationServices2Controller<TModule>(Action<ApplicationService2ControllerSetting>? optionsAction = null)
        where TModule : IFakeModule
    {
        var assembly = typeof(TModule).Assembly;
        if (ControllerSettings.Any(x => x.Assembly == assembly)) return;

        var setting = new ApplicationService2ControllerSetting(assembly);
        optionsAction?.Invoke(setting);
        setting.LoadControllers();
        ControllerSettings.Add(setting);
    }
}