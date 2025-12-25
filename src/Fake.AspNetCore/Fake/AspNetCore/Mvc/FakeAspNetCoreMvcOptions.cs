using Fake.AspNetCore.Mvc.Conventions;
using Fake.Modularity;

namespace Fake.AspNetCore.Mvc;

public class FakeAspNetCoreMvcOptions
{
    public List<ApplicationService2ControllerSetting> ConventionalControllerSettings { get; } = new();

    public void ApplicationServices2Controller<TModule>(Action<ApplicationService2ControllerSetting>? optionsAction = null)
        where TModule : IFakeModule
    {
        var assembly = typeof(TModule).Assembly;
        if (ConventionalControllerSettings.Any(x => x.Assembly == assembly)) return;

        var setting = new ApplicationService2ControllerSetting(assembly);
        optionsAction?.Invoke(setting);
        setting.LoadControllers();
        ConventionalControllerSettings.Add(setting);
    }
}