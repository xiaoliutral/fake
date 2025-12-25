using System.Reflection;
using Fake.Application;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Fake.AspNetCore.Mvc.Conventions;

public class ApplicationService2ControllerSetting
{
    internal HashSet<Type> ControllerTypes { get; } = new();
    internal Assembly Assembly { get; }

    private string _rootPath = null!;

    public string RootPath
    {
        get => _rootPath;
        set
        {
            ThrowHelper.ThrowIfNull(value, nameof(value));
            _rootPath = value;
        }
    }

    public Action<ControllerModel>? ControllerModelConfigureAction { get; set; }

    public Func<Type, bool>? TypePredicate { get; set; }

    public ApplicationService2ControllerSetting(Assembly assembly, string rootPath = "api")
    {
        Assembly = ThrowHelper.ThrowIfNull(assembly, nameof(assembly));
        RootPath = rootPath;
    }
    
    internal void LoadControllers()
    {
        var types = Assembly.GetTypes()
            .Where(IsApplicationService)
            .WhereIf(TypePredicate != null, TypePredicate!);

        foreach (var type in types)
        {
            ControllerTypes.Add(type);
        }
    }

    private static bool IsApplicationService(Type type)
    {
        if (!type.IsPublic || type.IsAbstract || type.IsGenericType)
        {
            return false;
        }

        if (typeof(IApplicationService).IsAssignableFrom(type))
        {
            return true;
        }

        return false;
    }
}